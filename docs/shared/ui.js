let deferredPrompt = null;
let isIOS = false;
let toastTimer = null;

function standaloneMode() {
  return (
    window.matchMedia("(display-mode: standalone)").matches ||
    window.matchMedia("(display-mode: fullscreen)").matches ||
    window.navigator.standalone === true
  );
}

export function isNativeShell() {
  return new URLSearchParams(window.location.search).has("native");
}

function buildInstallSheet() {
  const overlay = document.createElement("div");
  overlay.className = "install-overlay";
  overlay.innerHTML = `
    <section class="install-sheet" role="dialog" aria-modal="true" aria-labelledby="installTitle">
      <p class="sheet-kicker">MONOTASK</p>
      <h2 class="sheet-title" id="installTitle">安装到主屏幕</h2>
      <p class="sheet-copy"></p>
      <ol class="sheet-steps"></ol>
      <button class="sheet-btn" type="button">知道了</button>
    </section>
  `;
  document.body.appendChild(overlay);

  const close = () => {
    overlay.classList.remove("show");
    window.setTimeout(() => overlay.remove(), 200);
  };

  overlay.addEventListener("click", (event) => {
    if (event.target === overlay) close();
  });
  overlay.querySelector(".sheet-btn").addEventListener("click", close);
  document.addEventListener(
    "keydown",
    (event) => {
      if (event.key === "Escape") close();
    },
    { once: true }
  );
  return overlay;
}

function showInstallHelp() {
  const overlay = buildInstallSheet();
  const copy = overlay.querySelector(".sheet-copy");
  const steps = overlay.querySelector(".sheet-steps");

  if (isIOS) {
    copy.textContent = "使用 Safari 打开本页，添加到主屏幕后即可像 App 一样独立运行。";
    steps.innerHTML = `
      <li>点击 Safari 底部的“分享”按钮</li>
      <li>选择“添加到主屏幕”</li>
      <li>确认名称后点击“添加”</li>
    `;
  } else {
    copy.textContent = "在浏览器菜单中选择安装或添加到主屏幕，即可获得独立窗口和离线使用能力。";
    steps.innerHTML = `
      <li>打开浏览器右上角菜单</li>
      <li>选择“安装应用”或“添加到主屏幕”</li>
      <li>确认安装后从桌面启动</li>
    `;
  }

  requestAnimationFrame(() => overlay.classList.add("show"));
}

export function setupInstall(button) {
  if (!button) return;

  if (standaloneMode() || isNativeShell()) {
    button.hidden = true;
    return;
  }

  const ua = navigator.userAgent || "";
  isIOS =
    /iPad|iPhone|iPod/i.test(ua) ||
    (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);

  button.hidden = false;

  window.addEventListener("beforeinstallprompt", (event) => {
    event.preventDefault();
    deferredPrompt = event;
    button.hidden = false;
  });

  window.addEventListener("appinstalled", () => {
    deferredPrompt = null;
    button.hidden = true;
  });

  button.addEventListener("click", async () => {
    if (!deferredPrompt) {
      showInstallHelp();
      return;
    }
    const prompt = deferredPrompt;
    deferredPrompt = null;
    try {
      prompt.prompt();
      const choice = await prompt.userChoice;
      if (choice && choice.outcome === "accepted") {
        button.hidden = true;
      }
    } catch {
      showInstallHelp();
    }
  });
}

export function toast(message) {
  let element = document.querySelector(".toast");
  if (!element) {
    element = document.createElement("div");
    element.className = "toast";
    element.setAttribute("role", "status");
    element.setAttribute("aria-live", "polite");
    document.body.appendChild(element);
  }
  element.textContent = message;
  element.classList.add("show");
  window.clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => {
    element.classList.remove("show");
  }, 1800);
}

export function haptic(duration = 10) {
  if (navigator.vibrate) {
    try {
      navigator.vibrate(duration);
    } catch {}
  }
}
