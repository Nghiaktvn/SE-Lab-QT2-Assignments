# Northstar visual refresh — 23 October 2026

The shared header and footer now use a text-only NORTHSTAR wordmark with seven gently animated colors. The icon and WHOLESALE PARTNERS subtitle were removed at the user's request.

The home hero is enclosed in a rounded frame. A separate 30-second promotional film sits beneath both its copy and artwork. Nova appears as an original transparent 3D mascot in the launcher, assistant, catalog, About and Help pages. Product, order, account and SQL transaction rules are unchanged.

New public pages: `/about` and `/help`, with authentic product information and native expandable FAQs. Footer links point to working pages. No fabricated contact addresses, testimonials, delivery guarantees or self-registration flow were added.

Motion supports a global Pause effects control, reduced-motion preferences, off-screen video pausing and hidden-tab pausing. The film progress bar follows actual video time. Product selection gets a visible outline; the order summary remains the source of selected quantities.

Media: the web film is 1920×1080, 24 fps, 30 seconds. The 3840×2160 export is upscaled from that composition. The imagery is 3D-rendered artwork with composited character movement and title transitions; it is not a fully rigged character film. WebP derivatives reduce mascot download size. Original prompt and generator mode are recorded in `design/nova-v2-prompt.md`.

The existing report describes the same core software. This addendum records the later visual changes; screenshots and browser verification are saved alongside the existing evidence.

## Login and cross-lab design update

The partner login now follows the supplied Notezy reference's split editorial/form composition, pastel surfaces, rounded card and clear field/button hierarchy. The product-story panel uses the existing 30-second Nova media with an accessible pause control. The floating assistant stays out of the login form's way. Existing authentication, demo access, role checks and form validation remain intact. The native Admin sign-in also uses the same restrained cream/lilac/sage visual language without changing its credentials flow.

Lab 1's ticket machine applies all three palettes to the welcome and receipt surfaces as well as fields, labels and action buttons; primary and secondary buttons provide restrained hover and pressed feedback. The Lab 1 ticket, payment, receipt and reconciliation rules remain unchanged.

Editable design handoff: [Northstar Labs 1 + 2 UI in Figma](https://www.figma.com/design/XJaFLoJzp8NrexLNmOHBAk), with native frames for the Lab 1 kiosk and both Lab 2 sign-in screens. The app's own web UI remains Razor/CSS and the desktop apps remain WinForms to preserve the assignment stacks and rubric requirements.

After the final rebuild, the browser login was checked in the local demo: the 30-second MP4 loaded and played, the password visibility button toggled, demo access expanded/collapsed, Forest/Sunset themes applied and restored, and both per-film and global motion controls paused/resumed. The desktop login was freshly captured at `docs/screenshots/admin-login-v4.png`.
