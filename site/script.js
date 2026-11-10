/* LionSoft — interakcje i animacje. Wszystko jest progresywnym ulepszeniem:
   bez JS strona pozostaje w pełni czytelna. */
(function () {
  "use strict";

  var doc = document.documentElement;
  var reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var $ = function (s, c) { return (c || document).querySelector(s); };
  var $$ = function (s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); };
  var clamp = function (v, a, b) { return Math.max(a, Math.min(b, v)); };

  /* ---------- Nawigacja ---------- */
  var nav = $("#nav");
  var burger = $("#burger");
  var links = $$("#navLinks a");

  function closeMenu() {
    nav.classList.remove("open");
    burger.setAttribute("aria-expanded", "false");
  }
  if (burger) {
    burger.addEventListener("click", function () {
      var open = nav.classList.toggle("open");
      burger.setAttribute("aria-expanded", String(open));
    });
    links.forEach(function (a) { a.addEventListener("click", closeMenu); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeMenu(); });
  }

  /* Podświetlenie aktywnej sekcji */
  var sections = links
    .map(function (a) { return { a: a, el: document.getElementById(decodeURIComponent(a.hash.slice(1))) }; })
    .filter(function (s) { return s.el; });

  /* ---------- Efekty zależne od przewijania (jedna pętla rAF) ---------- */
  var steps = $("#steps");
  var stepEls = steps ? $$(".step", steps) : [];
  var motto = $("#motto");
  var words = [];

  if (motto && !reduced) {
    // Dzielimy motto na słowa, żeby "zapalały się" wraz z przewijaniem.
    var frag = document.createDocumentFragment();
    Array.prototype.slice.call(motto.childNodes).forEach(function (node) {
      var hl = node.nodeType === 1 && node.hasAttribute("data-hl");
      node.textContent.split(/(\s+)/).forEach(function (part) {
        if (!part) return;
        if (/^\s+$/.test(part)) { frag.appendChild(document.createTextNode(" ")); return; }
        var s = document.createElement("span");
        s.className = "w" + (hl ? " hl" : "");
        s.textContent = part;
        words.push(s);
        frag.appendChild(s);
      });
    });
    motto.textContent = "";
    motto.appendChild(frag);
  }

  var ticking = false;
  function onScroll() {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(update);
  }
  function update() {
    ticking = false;
    var y = window.scrollY;
    var vh = window.innerHeight;
    var max = doc.scrollHeight - vh;

    doc.style.setProperty("--scroll", max > 0 ? (y / max).toFixed(4) : 0);
    nav.classList.toggle("scrolled", y > 24);

    var current = null;
    sections.forEach(function (s) {
      if (s.el.getBoundingClientRect().top <= vh * 0.4) current = s;
    });
    sections.forEach(function (s) { s.a.classList.toggle("active", s === current); });

    if (steps) {
      var r = steps.getBoundingClientRect();
      var row = window.innerWidth > 1040;
      var p = clamp((vh * 0.8 - r.top) / (row ? vh * 0.5 : r.height), 0, 1);
      steps.style.setProperty("--p", p.toFixed(3));
      var n = stepEls.length - 1;
      stepEls.forEach(function (el, i) { el.classList.toggle("on", reduced || p * n >= i - 0.04); });
    }

    if (words.length) {
      var m = motto.getBoundingClientRect();
      var mp = clamp((vh * 0.88 - m.top) / (vh * 0.55), 0, 1);
      var lit = Math.round(mp * words.length);
      words.forEach(function (w, i) { w.classList.toggle("on", i < lit); });
    }
  }
  window.addEventListener("scroll", onScroll, { passive: true });
  window.addEventListener("resize", onScroll);
  update();

  /* ---------- Wejścia elementów ---------- */
  var revealEls = $$("[data-r]");
  if ("IntersectionObserver" in window && !reduced) {
    var io = new IntersectionObserver(function (entries) {
      // Elementy, które pojawiają się razem, wchodzą kaskadowo.
      var batch = entries.filter(function (e) { return e.isIntersecting; });
      batch.forEach(function (e, i) {
        e.target.style.setProperty("--i", i);
        e.target.classList.add("in");
        io.unobserve(e.target);
      });
    }, { threshold: 0.12, rootMargin: "0px 0px -6% 0px" });
    revealEls.forEach(function (el) { io.observe(el); });
  } else {
    revealEls.forEach(function (el) { el.classList.add("in"); });
  }

  /* ---------- Poświata podążająca za kursorem na kartach usług ---------- */
  $$(".svc").forEach(function (card) {
    card.addEventListener("pointermove", function (e) {
      var r = card.getBoundingClientRect();
      card.style.setProperty("--mx", (e.clientX - r.left) + "px");
      card.style.setProperty("--my", (e.clientY - r.top) + "px");
    });
  });

  /* ---------- Paralaksa sceny w hero ---------- */
  var stage = $("#stage");
  var hero = $(".hero");
  if (stage && hero && !reduced && window.matchMedia("(pointer:fine)").matches) {
    hero.addEventListener("pointermove", function (e) {
      var r = hero.getBoundingClientRect();
      stage.style.setProperty("--px", ((e.clientX - r.left) / r.width - 0.5).toFixed(3));
      stage.style.setProperty("--py", ((e.clientY - r.top) / r.height - 0.5).toFixed(3));
    });
    hero.addEventListener("pointerleave", function () {
      stage.style.setProperty("--px", 0);
      stage.style.setProperty("--py", 0);
    });
  }

  /* ---------- "Pisanie" kodu ---------- */
  var code = $("#code");
  if (code && !reduced) {
    var tokens = Array.prototype.slice.call(code.childNodes).map(function (n) {
      return { cls: n.nodeType === 1 ? n.className : "", text: n.textContent };
    });
    code.style.height = code.offsetHeight + "px";
    code.textContent = "";
    var caret = document.createElement("span");
    caret.className = "caret";
    code.appendChild(caret);

    var ti = 0, ci = 0, target = null;
    var type = function () {
      if (ti >= tokens.length) return;
      var t = tokens[ti];
      if (ci === 0) {
        target = t.cls ? document.createElement("span") : document.createTextNode("");
        if (t.cls) target.className = t.cls;
        code.insertBefore(target, caret);
      }
      // Wcięcia i znaki nowej linii wstawiamy od razu — pisze się tylko "treść".
      var chunk = t.text.charAt(ci++);
      while (ci < t.text.length && /\s/.test(chunk.slice(-1)) && /\s/.test(t.text.charAt(ci))) chunk += t.text.charAt(ci++);
      target.textContent += chunk;
      if (ci >= t.text.length) { ti++; ci = 0; }
      setTimeout(type, 14 + Math.random() * 34);
    };
    setTimeout(type, 900);
  }

  /* ---------- Tło hero: sieć połączeń na canvasie ---------- */
  var canvas = $("#net");
  if (canvas && canvas.getContext) {
    var ctx = canvas.getContext("2d");
    var pts = [], W = 0, H = 0, dpr = 1, running = false, raf = 0;
    var mouse = { x: -9999, y: -9999 };
    var LINK = 150;

    var resize = function () {
      var r = canvas.getBoundingClientRect();
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      W = r.width; H = r.height;
      canvas.width = W * dpr; canvas.height = H * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      var count = clamp(Math.round(W * H / 17000), 24, 90);
      pts = [];
      for (var i = 0; i < count; i++) {
        pts.push({
          x: Math.random() * W, y: Math.random() * H,
          vx: (Math.random() - 0.5) * 0.28, vy: (Math.random() - 0.5) * 0.28,
          r: Math.random() * 1.6 + 0.6, gold: Math.random() < 0.35
        });
      }
    };

    var draw = function () {
      ctx.clearRect(0, 0, W, H);
      var i, j, a, b, dx, dy, d;
      for (i = 0; i < pts.length; i++) {
        a = pts[i];
        a.x += a.vx; a.y += a.vy;
        if (a.x < 0 || a.x > W) a.vx *= -1;
        if (a.y < 0 || a.y > H) a.vy *= -1;

        for (j = i + 1; j < pts.length; j++) {
          b = pts[j];
          dx = a.x - b.x; dy = a.y - b.y;
          d = Math.sqrt(dx * dx + dy * dy);
          if (d < LINK) {
            ctx.strokeStyle = "rgba(140,165,225," + (0.16 * (1 - d / LINK)).toFixed(3) + ")";
            ctx.lineWidth = 1;
            ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
          }
        }
        dx = a.x - mouse.x; dy = a.y - mouse.y;
        d = Math.sqrt(dx * dx + dy * dy);
        if (d < 200) {
          ctx.strokeStyle = "rgba(245,185,66," + (0.4 * (1 - d / 200)).toFixed(3) + ")";
          ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(mouse.x, mouse.y); ctx.stroke();
        }
        ctx.fillStyle = a.gold ? "rgba(245,185,66,.8)" : "rgba(150,175,235,.55)";
        ctx.beginPath(); ctx.arc(a.x, a.y, a.r, 0, 6.2832); ctx.fill();
      }
    };
    var loop = function () { draw(); if (running) raf = requestAnimationFrame(loop); };
    var start = function () { if (!running && !reduced) { running = true; loop(); } };
    var stop = function () { running = false; cancelAnimationFrame(raf); };

    resize();
    draw();
    window.addEventListener("resize", function () { resize(); draw(); });
    hero.addEventListener("pointermove", function (e) {
      var r = canvas.getBoundingClientRect();
      mouse.x = e.clientX - r.left; mouse.y = e.clientY - r.top;
    });
    hero.addEventListener("pointerleave", function () { mouse.x = mouse.y = -9999; });

    // Animujemy tylko wtedy, gdy hero jest widoczne, a karta aktywna.
    if ("IntersectionObserver" in window) {
      new IntersectionObserver(function (e) { e[0].isIntersecting ? start() : stop(); }).observe(canvas);
    } else { start(); }
    document.addEventListener("visibilitychange", function () {
      if (document.hidden) stop();
      else if (canvas.getBoundingClientRect().bottom > 0) start();
    });
  }

  /* ---------- Kopiowanie adresu e-mail ---------- */
  var copy = $("#copy");
  if (copy) {
    if (!navigator.clipboard) copy.hidden = true;
    var label = copy.textContent;
    copy.addEventListener("click", function () {
      navigator.clipboard.writeText(copy.dataset.email).then(function () {
        copy.textContent = copy.dataset.done;
        copy.classList.add("done");
        setTimeout(function () { copy.textContent = label; copy.classList.remove("done"); }, 2200);
      });
    });
  }

  var year = $("#year");
  if (year) year.textContent = new Date().getFullYear();
})();
