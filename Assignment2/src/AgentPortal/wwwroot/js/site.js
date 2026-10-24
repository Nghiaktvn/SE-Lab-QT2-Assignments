(() => {
const money=value=>new Intl.NumberFormat('en-US',{maximumFractionDigits:0}).format(value)+' VND';
document.querySelector('[data-toggle-password]')?.addEventListener('click',event=>{
  const input=document.querySelector('#Password');
  input.type=input.type==='password'?'text':'password';
  event.currentTarget.textContent=input.type==='password'?'Show':'Hide';
  event.currentTarget.setAttribute('aria-label',input.type==='password'?'Show password':'Hide password');
});
const form=document.querySelector('[data-order-form]');
if(form){
 const inputs=[...form.querySelectorAll('.quantity')];
 const render=()=>{
  const selected=inputs.filter(input=>Number(input.value)>0),units=selected.reduce((s,i)=>s+Number(i.value),0),total=selected.reduce((s,i)=>s+Number(i.value)*Number(i.dataset.price),0);
  inputs.forEach(input=>input.closest('[data-product]').classList.toggle('is-selected',Number(input.value)>0));
  const target=document.querySelector('#selected-lines');target.replaceChildren();
  if(!selected.length){const p=document.createElement('p');p.textContent='No products selected yet.';target.append(p);}
  selected.forEach(input=>{
    const row=document.createElement('div'),name=document.createElement('span'),amount=document.createElement('strong');
    name.textContent=input.closest('tr').querySelector('strong').textContent+' × '+input.value;
    amount.textContent=money(Number(input.value)*Number(input.dataset.price));
    row.append(name,amount);target.append(row);
  });
  document.querySelector('#line-count').textContent=selected.length;document.querySelector('#unit-count').textContent=units;document.querySelector('#order-total').textContent=money(total);
  document.querySelector('#order-live').textContent=selected.length+' product lines, '+units+' units, '+money(total);
 };
 inputs.forEach(input=>input.addEventListener('input',render));
 form.querySelectorAll('[data-step]').forEach(button=>button.addEventListener('click',()=>{
   const input=button.parentElement.querySelector('input');if(input.disabled)return;
   input.value=Math.min(Number(input.max),Math.max(0,(Number(input.value)||0)+Number(button.dataset.step)));
   render();
 }));
 document.querySelector('#clear-order').addEventListener('click',()=>{inputs.forEach(input=>input.value=0);render();});
 form.addEventListener('submit',event=>{
   if(!inputs.some(input=>Number(input.value)>0)){
     event.preventDefault();inputs.find(input=>!input.disabled)?.focus();
     document.querySelector('#order-live').textContent='Select a quantity for at least one product.';return;
   }
   const submit=form.querySelector('button[type=submit]');submit.disabled=true;submit.textContent='Placing your order…';
 });
 window.addEventListener('pageshow',()=>{
   const submit=form.querySelector('button[type=submit]');submit.disabled=false;submit.textContent='Place order →';
 });
 render();
}
document.querySelector('#order-status-filter')?.addEventListener('change',event=>{
  const rows=[...document.querySelectorAll('[data-order-status]')];
  rows.forEach(row=>row.hidden=!!event.target.value&&row.dataset.orderStatus!==event.target.value);
  document.querySelector('#no-status-orders').hidden=rows.some(row=>!row.hidden);
});
})();

(() => {
 const form=document.querySelector('[data-payment-form]');
 if(!form)return;
 const submit=form.querySelector('button[type="submit"]');
 const label=submit.textContent;
 form.addEventListener('submit',()=>{
   submit.disabled=true;
   submit.textContent='Confirming payment…';
   form.setAttribute('aria-busy','true');
 });
 window.addEventListener('pageshow',()=>{
   submit.disabled=false;
   submit.textContent=label;
   form.removeAttribute('aria-busy');
 });
})();

(() => {
 const reduced=window.matchMedia('(prefers-reduced-motion: reduce)');
 const clips=[...document.querySelectorAll('[data-motion]')];
 let motionEnabled=!reduced.matches;
 try{if(sessionStorage.getItem('northstar-motion')==='paused')motionEnabled=false;}catch{}
 const visible=new Set(),pausedByUser=new Set(),allToggle=document.querySelector('#toggle-all-motion');
 clips.filter(video=>video.hasAttribute('data-motion-manual')).forEach(video=>pausedByUser.add(video));
 const getBtn=video=>video.closest('.hero-video-wrapper, .film-screen, .catalog-mascot, .film-container')?.querySelector('[data-motion-toggle]')||video.parentElement.querySelector('[data-motion-toggle]');
 const syncButton=video=>{
   const button=getBtn(video);
   const host=video.closest('.hero-video-wrapper')||video.parentElement;
   host.classList.toggle('motion-paused',video.paused);
   if(button){
     button.textContent=video.paused?'Play animation':'Pause animation';
     button.setAttribute('aria-pressed',String(!video.paused));
   }
 };
 const sync=()=>{
  document.documentElement.toggleAttribute('data-motion-paused',!motionEnabled||document.hidden);
  if(allToggle){
    allToggle.textContent=motionEnabled?'Pause effects':'Play effects';
    allToggle.setAttribute('aria-pressed',String(motionEnabled));
  }
  clips.forEach(video=>{
    if(motionEnabled&&!document.hidden&&visible.has(video)&&!pausedByUser.has(video))video.play().catch(()=>syncButton(video));
    else video.pause();
    syncButton(video);
  });
 };
 clips.forEach(video=>{
   video.addEventListener('play',()=>syncButton(video));
   video.addEventListener('pause',()=>syncButton(video));
   video.addEventListener('timeupdate',()=>{
     const progress=video.duration>0?video.currentTime/video.duration:0;
     const host=video.closest('.hero-video-wrapper')||video.parentElement;
     host.style.setProperty('--film-progress',String(progress));
   });
   const toggleBtn=getBtn(video);
   toggleBtn?.addEventListener('click',()=>{
     if(video.paused){pausedByUser.delete(video);motionEnabled=true;}
     else pausedByUser.add(video);
     sync();
   });
 });
 if('IntersectionObserver' in window){
   const observer=new IntersectionObserver(entries=>{
     entries.forEach(entry=>entry.isIntersecting?visible.add(entry.target):visible.delete(entry.target));
     sync();
   },{threshold:.12});
   clips.forEach(video=>observer.observe(video));
 }else clips.forEach(video=>visible.add(video));
 allToggle?.addEventListener('click',()=>{
   motionEnabled=!motionEnabled;
   if(motionEnabled)pausedByUser.clear();
   try{sessionStorage.setItem('northstar-motion',motionEnabled?'playing':'paused');}catch{}
   sync();
 });
 reduced.addEventListener('change',()=>{motionEnabled=!reduced.matches;sync();});
 document.addEventListener('visibilitychange',sync);sync();
 const form=document.querySelector('[data-order-form]');
 if(!form)return;
 const rows=[...form.querySelectorAll('[data-product]')];let category='';
 const filter=()=>{
   const term=document.querySelector('#catalog-search').value.trim().toLowerCase();
   rows.forEach(row=>row.hidden=!row.dataset.search.includes(term)||(category!==''&&row.dataset.category!==category));
   form.querySelector('.empty-search').hidden=rows.some(row=>!row.hidden);
 };
 document.querySelectorAll('[data-category-filter]').forEach(button=>button.addEventListener('click',()=>{
   category=button.dataset.categoryFilter;
   document.querySelectorAll('[data-category-filter]').forEach(b=>{b.classList.toggle('active',b===button);b.setAttribute('aria-pressed',String(b===button));});
   filter();
 }));
 document.querySelector('#catalog-search').addEventListener('input',filter);
 const initialCategory=new URLSearchParams(location.search).get('category');
 const initialButton=[...document.querySelectorAll('[data-category-filter]')].find(b=>b.dataset.categoryFilter===initialCategory);
 if(initialButton)initialButton.click();
 document.querySelectorAll('[data-catalog-view]').forEach(button=>button.addEventListener('click',()=>{
   document.querySelector('.catalog-table').classList.toggle('catalog-grid',button.dataset.catalogView==='grid');
   document.querySelectorAll('[data-catalog-view]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
 }));
})();


(() => {
 const picker=document.querySelector('#color-palette');
 if(picker){
   let saved='forest';try{saved=localStorage.getItem('northstar-color')||'forest';}catch{}
   if(!['forest','sunset','lilac'].includes(saved))saved='forest';
   document.documentElement.dataset.palette=saved;picker.value=saved;
   picker.addEventListener('change',()=>{
     document.documentElement.dataset.palette=picker.value;
     try{localStorage.setItem('northstar-color',picker.value);}catch{}
   });
 }
 const dialog=document.querySelector('#nova-assistant'),form=document.querySelector('#assistant-form'),question=document.querySelector('#assistant-question'),messages=document.querySelector('#assistant-messages');
 const bubble=document.querySelector('#assistant-bubble');
 if(bubble){
   setTimeout(()=>{bubble.style.transition='opacity .6s ease';bubble.style.opacity='0.95';},1200);
   bubble.addEventListener('click',()=>{bubble.style.display='none';});
 }
 if(!dialog||!form)return;
 document.querySelectorAll('[data-open-assistant]').forEach(button=>button.addEventListener('click',()=>{
   if(bubble)bubble.style.display='none';
   if(!dialog.open)dialog.showModal();
   question.focus();
 }));
 document.querySelector('#close-assistant')?.addEventListener('click',()=>dialog.close());
 dialog.addEventListener('click',event=>{
   if(event.target===dialog){
     const r=dialog.getBoundingClientRect();
     if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close();
   }
 });
 const append=(text,kind)=>{
   const p=document.createElement('p');p.className=kind;p.textContent=text;
   messages.append(p);
   while(messages.children.length>12)messages.firstElementChild.remove();
   messages.scrollTop=messages.scrollHeight;
 };
 let waiting=false;
 form.addEventListener('submit',async event=>{
  event.preventDefault();if(waiting||!question.value.trim())return;
  const payload=new FormData(form);append(question.value.trim(),'user-message');
  question.value='';waiting=true;
  const button=form.querySelector('button');button.disabled=true;
  form.setAttribute('aria-busy','true');
  document.querySelector('#assistant-typing').hidden=false;
  const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),15000);
  try{
    const response=await fetch(form.action,{method:'POST',body:payload,signal:controller.signal,credentials:'same-origin',headers:{Accept:'application/json'}});
    if(!response.ok)throw new Error('Unavailable');
    const answer=await response.json();
    append(answer.reply,'bot-message');
    for(const item of answer.items||[]){
      const link=document.createElement('a');link.className='assistant-product';link.href=item.link;
      const name=document.createElement('strong');name.textContent=item.name;
      link.append(name,document.createTextNode(new Intl.NumberFormat('en-US').format(item.price)+' VND · '+item.stock+' in stock ↗'));
      messages.append(link);
    }
    if(answer.link){
      const link=document.createElement('a');link.className='assistant-product';link.href=answer.link;
      link.textContent=answer.linkText+' ↗';messages.append(link);
    }
    messages.scrollTop=messages.scrollHeight;
  }catch{
    append('The helper is temporarily unavailable. Please retry or use the catalog and My orders.','bot-message');
  }finally{
    clearTimeout(timeout);waiting=false;button.disabled=false;form.removeAttribute('aria-busy');
    document.querySelector('#assistant-typing').hidden=true;
    if(dialog.open)question.focus();
  }
 });
 document.querySelectorAll('[data-assistant-prompt]').forEach(button=>button.addEventListener('click',()=>{
   question.value=button.dataset.assistantPrompt;form.requestSubmit();
 }));
 const initial=new URLSearchParams(location.search).get('search');
 if(initial&&document.querySelector('#catalog-search')){
   const search=document.querySelector('#catalog-search');
   search.value=initial;search.dispatchEvent(new Event('input',{bubbles:true}));
 }
})();

(() => {
 const search=document.querySelector('#catalog-search');
 if(search){
   document.addEventListener('keydown',event=>{
     if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='k'){event.preventDefault();search.focus();search.select();}
   });
   search.addEventListener('keydown',event=>{
     if(event.key==='Escape'){search.value='';search.dispatchEvent(new Event('input',{bubbles:true}));}
   });
 }
 document.querySelectorAll('[data-category-filter]').forEach(button=>button.setAttribute('aria-pressed',String(button.classList.contains('active'))));
 if('IntersectionObserver' in window&&!window.matchMedia('(prefers-reduced-motion: reduce)').matches){
   const reveal=new IntersectionObserver(entries=>entries.forEach(entry=>{
     if(entry.isIntersecting){entry.target.classList.add('is-visible');reveal.unobserve(entry.target);}
   }),{threshold:.08});
   document.querySelectorAll('[data-reveal]').forEach(section=>{section.classList.add('js-reveal');reveal.observe(section);});
 }

 /* Subtle 3D mouse parallax on hero frame */
 const hero=document.querySelector('#hero-frame');
 const assistantWidget=document.querySelector('.assistant-widget');
 if(hero&&assistantWidget&&window.matchMedia('(max-width:700px)').matches&&'IntersectionObserver' in window){
   assistantWidget.classList.add('assistant-hidden-during-hero');
   assistantWidget.inert=true;
   const assistantObserver=new IntersectionObserver(([entry])=>{
     assistantWidget.classList.toggle('assistant-hidden-during-hero',entry.isIntersecting);
     assistantWidget.inert=entry.isIntersecting;
   },{threshold:0});
   assistantObserver.observe(hero);
 }
 if(hero&&!window.matchMedia('(prefers-reduced-motion: reduce)').matches){
   const char=hero.querySelector('.bg-nova');
   const orbs=[...hero.querySelectorAll('.bg-orb')];
   hero.addEventListener('mousemove',e=>{
     const rect=hero.getBoundingClientRect();
     const x=(e.clientX-rect.left)/rect.width-0.5;
     const y=(e.clientY-rect.top)/rect.height-0.5;
     if(char)char.style.transform=`translate3d(${x*18}px, ${y*12}px, 0) rotate(${x*4}deg)`;
     orbs.forEach((orb,idx)=>{
       const factor=(idx+1)*12;
       orb.style.transform=`translate3d(${x*factor}px, ${y*factor}px, 0)`;
     });
   });
   hero.addEventListener('mouseleave',()=>{
     if(char)char.style.transform='';
     orbs.forEach(orb=>orb.style.transform='');
   });
 }
})();
