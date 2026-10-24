const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '..');
const output = path.join(root, 'docs', 'evidence');
const base = process.env.PORTAL_URL || 'http://127.0.0.1:5400';
const chrome = process.env.CHROME_PATH || 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const account = process.env.DEMO_AGENT_EMAIL || 'agent01@wholesale.local';
const password = process.env.DEMO_AGENT_PASSWORD || 'Demo123!';
const productIds = (process.env.SMOKE_PRODUCT_IDS || '').split(',').filter(Boolean);
const prefix = productIds.length ? 'browser-fixture' : 'paid-invoice';

(async () => {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ executablePath: chrome, headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 });
  const errors = [];
  const checks = [];
  const check = (condition, name) => {
    if (!condition) throw new Error(name);
    checks.push(name);
  };
  page.on('pageerror', error => errors.push(error.message));
  try {
    await page.goto(new URL('/', base).href);
    check(await page.getByRole('heading', { name: /a little goodness/i }).count() > 0, 'Home content');
    await page.locator('header nav').getByRole('link', { name: 'About' }).click();
    check(new URL(page.url()).pathname === '/about', 'About navigation');
    await page.locator('header nav').getByRole('link', { name: 'Help' }).click();
    check(new URL(page.url()).pathname === '/help', 'Help navigation');
    await page.locator('header .brand').click();
    check(new URL(page.url()).pathname === '/', 'Brand home navigation');
    await page.locator('#toggle-all-motion').click();
    check(await page.locator('#toggle-all-motion').getAttribute('aria-pressed') === 'false', 'Pause effects');
    await page.locator('#toggle-all-motion').click();
    check(await page.locator('#toggle-all-motion').getAttribute('aria-pressed') === 'true', 'Resume effects');
    await page.locator('#color-palette').selectOption('lilac');
    check(await page.locator('html').getAttribute('data-palette') === 'lilac', 'Color palette');
    await page.locator('#open-assistant').click();
    check(await page.locator('#nova-assistant').evaluate(node => node.open), 'Open assistant');
    await page.locator('#assistant-question').fill('help');
    await page.getByRole('button', { name: 'Send question' }).click();
    await page.waitForFunction(() => document.querySelectorAll('#assistant-messages .bot-message').length > 1);
    check((await page.locator('#assistant-messages .bot-message').count()) > 1, 'Assistant answer');
    await page.locator('#close-assistant').click();
    check(!(await page.locator('#nova-assistant').evaluate(node => node.open)), 'Close assistant');
    await page.locator('header').getByRole('link', { name: /partner sign in/i }).click();
    check(new URL(page.url()).pathname === '/login', 'Login navigation');
    await page.locator('#Password').fill(password);
    await page.locator('[data-toggle-password]').click();
    check(await page.locator('#Password').getAttribute('type') === 'text', 'Show password');
    await page.locator('[data-toggle-password]').click();
    check(await page.locator('#Password').getAttribute('type') === 'password', 'Hide password');
    await page.locator('#Email').fill(account);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForTimeout(900);
    check(page.url().endsWith('/catalog'), `Agent sign-in landed on catalog: ${page.url()}`);
    check((await page.locator('[data-product]').count()) > 0, 'Catalog loads products');
    await page.locator('#open-assistant').click();
    for (const prompt of ['coffee under 250k', 'my order status', 'low stock', 'pantry essentials']) {
      await page.waitForFunction(() => !document.querySelector('#assistant-form').hasAttribute('aria-busy'));
      await page.locator(`[data-assistant-prompt="${prompt}"]`).click();
      await page.waitForFunction(value => {
        const user = [...document.querySelectorAll('#assistant-messages .user-message')].at(-1);
        const reply = [...document.querySelectorAll('#assistant-messages .bot-message')].at(-1);
        return user?.textContent === value && !document.querySelector('#assistant-form').hasAttribute('aria-busy') && !!reply?.textContent;
      }, prompt);
      check((await page.locator('#assistant-messages .bot-message').last().innerText()).trim().length > 0, `Assistant prompt: ${prompt}`);
    }
    await page.locator('#close-assistant').click();
    const firstAvailable = page.locator('[data-product] input.quantity:not([disabled])').first();
    const firstRow = firstAvailable.locator('xpath=ancestor::tr');
    const firstName = (await firstRow.locator('.product-cell strong').innerText()).trim();
    const firstCategory = await firstRow.getAttribute('data-category');
    await page.locator(`[data-category-filter="${firstCategory}"]`).click();
    check((await page.locator('[data-product]:visible').count()) > 0, 'Category filter');
    await page.locator('[data-category-filter=""]').click();
    await page.locator('#catalog-search').fill(firstName);
    check((await page.locator('[data-product]:visible').count()) > 0, 'Product search');
    await page.locator('#catalog-search').fill('zz-no-such-product-zz');
    check(await page.locator('.empty-search').isVisible(), 'Empty search state');
    await page.locator('#catalog-search').fill('');
    await page.locator('[data-catalog-view="list"]').click();
    check(await page.locator('[data-catalog-view="list"]').getAttribute('aria-pressed') === 'true', 'List view');
    await page.locator('[data-catalog-view="grid"]').click();
    check(await page.locator('[data-catalog-view="grid"]').getAttribute('aria-pressed') === 'true', 'Card view');
    await firstRow.locator('[data-step="1"]').click();
    check(Number(await firstAvailable.inputValue()) === 1, 'Increase quantity');
    await firstRow.locator('[data-step="-1"]').click();
    check(Number(await firstAvailable.inputValue()) === 0, 'Decrease quantity');
    await firstRow.locator('[data-step="1"]').click();
    await page.locator('#clear-order').click();
    check((await page.locator('#line-count').innerText()).trim() === '0', 'Clear selection');
    await page.getByRole('button', { name: /place order/i }).click();
    check(page.url().endsWith('/catalog'), 'Empty checkout prevented');
    if (productIds.length === 2) {
      for (const id of productIds) await page.locator(`input[name="qty_${id}"]`).fill('1');
      if (!(await page.locator('#line-count').textContent())?.includes('2')) throw new Error('Two-line cart summary did not update');
      await page.getByRole('button', { name: /place order/i }).click();
      await page.waitForURL('**/invoice');
      if (await page.locator('.invoice tbody tr').count() !== 2) throw new Error('Checkout did not create two invoice lines');
      await page.getByRole('button', { name: /confirm payment/i }).click();
      await page.waitForLoadState('networkidle');
      if (!(await page.locator('.invoice-meta .badge-paid').count())) throw new Error('Payment confirmation was not persisted');
    } else {
      await page.goto(new URL('/orders', base).href);
      await page.locator('#order-status-filter').selectOption('Cancelled');
      check((await page.locator('[data-order-status]:visible').count()) === (await page.locator('[data-order-status="Cancelled"]').count()), 'Order status filter');
      await page.locator('#order-status-filter').selectOption('');
      const paid = page.locator('tr:has(.badge-paid)').first();
      if (!(await paid.count())) throw new Error('No paid classroom invoice is available for the browser smoke check');
      await paid.getByRole('link', { name: /view invoice/i }).click();
      await page.waitForURL('**/invoice');
    }
    const invoicePath = new URL(page.url()).pathname;
    await page.evaluate(() => { window.__printCalled = false; window.print = () => { window.__printCalled = true; }; });
    await page.getByRole('button', { name: /print invoice/i }).click();
    check(await page.evaluate(() => window.__printCalled), 'Print invoice button');
    const reference = (await page.locator('.invoice-meta .invoice-reference').textContent())?.trim();
    const contact = (await page.locator('.invoice-contact').textContent())?.trim();
    if (!reference?.startsWith('DEMO-') || !contact) throw new Error('Printable payment reference or agent contact is missing');
    if (productIds.length) {
      await page.goto(new URL('/orders', base).href);
      const row = page.locator(`a[href="${invoicePath}"]`).locator('xpath=ancestor::tr');
      if (!(await row.locator('.badge-paid').count())) throw new Error('My Orders does not show the paid two-line order');
      await page.goto(new URL(invoicePath, base).href);
    }
    await page.screenshot({ path: path.join(output, `${prefix}-desktop.png`), fullPage: true });
    await page.pdf({ path: path.join(output, `${prefix}-print.pdf`), format: 'A4', printBackground: true });
    await page.setViewportSize({ width: 304, height: 760 });
    await page.screenshot({ path: path.join(output, `${prefix}-mobile-304.png`), fullPage: true });
    const metrics = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth, invoice: Math.ceil(document.querySelector('.invoice').getBoundingClientRect().width) }));
    if (metrics.document > metrics.viewport + 1) throw new Error(`Mobile horizontal overflow: ${JSON.stringify(metrics)}`);
    if (await page.locator('.assistant-widget').isVisible()) throw new Error('Floating helper obscures the mobile invoice');
    const manifest = path.join(output, 'sample-shipping-manifest.html');
    if (fs.existsSync(manifest)) {
      await page.goto(pathToFileURL(manifest).href);
      await page.pdf({ path: path.join(output, 'shipping-manifest-print.pdf'), format: 'A4', printBackground: true });
    }
    if (errors.length) throw new Error(`Browser JavaScript errors: ${errors.join('; ')}`);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(new URL(invoicePath, base).href);
    await page.locator('.invoice-actions').getByRole('link', { name: /back to my orders/i }).click();
    check(new URL(page.url()).pathname === '/orders', 'Invoice back navigation');
    await page.locator('header').getByRole('button', { name: /sign out/i }).click();
    check(new URL(page.url()).pathname === '/login', 'Sign out');
    await page.goto(new URL('/orders', base).href);
    check(new URL(page.url()).pathname === '/login', 'Protected orders redirect');
    const results = { status: 'PASS', checks, invoice: invoicePath, reference, agentContact: contact, mobile: metrics, browserErrors: errors.length, manifestPdf: fs.existsSync(path.join(output, 'shipping-manifest-print.pdf')) };
    fs.writeFileSync(path.join(output, `${prefix}-smoke.json`), JSON.stringify(results, null, 2));
    console.log(JSON.stringify(results, null, 2));
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
