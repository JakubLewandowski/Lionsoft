// LionSoft — lionsoft.pl/kancelaria: animacje wejścia, kalendarz terminu, wpisywanie KRS, oś procesu.
(() => {
  const reduce = matchMedia("(prefers-reduced-motion: reduce)").matches;
  const $ = (s, r = document) => r.querySelector(s);
  const $$ = (s, r = document) => [...r.querySelectorAll(s)];

  const year = $("#year");
  if (year) year.textContent = new Date().getFullYear();

  // Kalendarz: listopad 2026, doręczenie 4.11, termin 2 tygodnie → 18.11, przypomnienia 7 i 2 dni wcześniej.
  const cal = $("#cal");
  const calDays = [];
  if (cal) {
    const y = 2026, m = 10, start = 4, end = 18, reminders = [11, 16];
    const html = ["Pn", "Wt", "Śr", "Cz", "Pt", "So", "Nd"].map(d => `<span class="dow">${d}</span>`);
    const lead = (new Date(y, m, 1).getDay() + 6) % 7;
    for (let i = 0; i < lead; i++) html.push("<span></span>");
    const last = new Date(y, m + 1, 0).getDate();
    for (let d = 1; d <= last; d++) html.push(`<span class="d" data-d="${d}">${d}</span>`);
    cal.innerHTML = html.join("");
    $$(".d", cal).forEach(el => {
      const d = +el.dataset.d;
      if (d === start) el.classList.add("start");
      if (d > start && d <= end) calDays.push(el);
      if (reminders.includes(d)) el.dataset.rem = "1";
    });
    const fill = () => calDays.forEach((el, i) => {
      const d = +el.dataset.d;
      const apply = () => {
        el.classList.add(d === end ? "end" : "range");
        if (el.dataset.rem) el.classList.add("rem");
      };
      reduce ? apply() : setTimeout(apply, 300 + i * 70);
    });
    cal.fillRange = fill;
  }

  // Wpisywanie numeru KRS, potem uzupełnienie pozostałych pól.
  const typeKrs = form => {
    const b = $(".type", form);
    const text = b.dataset.text;
    if (reduce) { b.textContent = text; form.classList.add("filled"); return; }
    let i = 0;
    const tick = () => {
      b.textContent = text.slice(0, ++i);
      if (i < text.length) setTimeout(tick, 70 + Math.random() * 60);
      else setTimeout(() => form.classList.add("filled"), 350);
    };
    setTimeout(tick, 500);
  };

  // Wejścia przy przewijaniu.
  const onIn = el => {
    el.classList.add("in");
    const form = $(".form", el);
    if (form) typeKrs(form);
    if (cal && el.contains(cal)) cal.fillRange();
  };
  const targets = $$("[data-r]");
  if (reduce || !("IntersectionObserver" in window)) {
    targets.forEach(onIn);
  } else {
    const io = new IntersectionObserver(entries => {
      entries.forEach(e => {
        if (!e.isIntersecting) return;
        onIn(e.target);
        io.unobserve(e.target);
      });
    }, { threshold: 0.18, rootMargin: "0px 0px -6% 0px" });
    targets.forEach(el => io.observe(el));
  }

  // Pasek postępu, cień nawigacji i oś procesu wypełniana w trakcie przewijania.
  const nav = $("#nav");
  const steps = $("#steps");
  const stepItems = steps ? $$("li", steps) : [];
  let ticking = false;
  const update = () => {
    ticking = false;
    const doc = document.documentElement;
    const max = doc.scrollHeight - innerHeight;
    doc.style.setProperty("--p", max > 0 ? (scrollY / max).toFixed(4) : 0);
    nav.classList.toggle("scrolled", scrollY > 8);
    if (steps) {
      const r = steps.getBoundingClientRect();
      const p = reduce ? 1 : Math.min(1, Math.max(0, (innerHeight * 0.75 - r.top) / (r.height + innerHeight * 0.15)));
      steps.style.setProperty("--rail", p.toFixed(3));
      stepItems.forEach((li, i) => li.classList.toggle("lit", p >= i / (stepItems.length - 1) - 0.02));
    }
  };
  addEventListener("scroll", () => { if (!ticking) { ticking = true; requestAnimationFrame(update); } }, { passive: true });
  addEventListener("resize", update);
  update();
})();
