import { createListStore } from "../shared/store.js";
import { setupInstall, haptic } from "../shared/ui.js";

const store = createListStore({
  key: "monotask.weeder.v1",
  listId: "weeder"
});

const listEl = document.getElementById("list");
const countEl = document.getElementById("count");
const clearBtn = document.getElementById("clearBtn");
const composer = document.getElementById("composer");
const input = document.getElementById("input");
const addBtn = document.getElementById("addBtn");
const installBtn = document.getElementById("installBtn");
const rowTemplate = document.getElementById("rowTemplate");

let undoSnapshot = null;
let undoCount = 0;
let undoTimer = null;
let scrollToBottom = false;

setupInstall(installBtn);

function createEmptyState() {
  const element = document.createElement("div");
  element.className = "empty-state";
  element.innerHTML = `
    <div class="empty-icon">
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="m5 12.5 4.2 4.2L19 7.3"></path>
      </svg>
    </div>
    <p class="empty-title">没有杂草</p>
    <p class="empty-hint">添加事项后，勾选完成项并一键清除。清除后 6 秒内可撤回。</p>
  `;
  return element;
}

function countChecked(items) {
  return items.reduce((total, item) => total + (item.done ? 1 : 0), 0);
}

function updateAction(items) {
  if (undoSnapshot) {
    clearBtn.disabled = false;
    clearBtn.classList.add("undo");
    clearBtn.textContent = "已清除 " + undoCount + " 项 · 点此撤回";
    return;
  }

  const checked = countChecked(items);
  clearBtn.classList.remove("undo");
  clearBtn.disabled = checked === 0;
  clearBtn.textContent = checked ? "一键清除已勾选 · " + checked : "暂无已勾选项";
}

function render() {
  const previousScroll = listEl.scrollTop;
  const items = store.items;

  countEl.textContent = items.length + " 项";
  updateAction(items);
  listEl.textContent = "";

  if (!items.length) {
    listEl.appendChild(createEmptyState());
  } else {
    const fragment = document.createDocumentFragment();
    items.forEach((item) => {
      const row = rowTemplate.content.firstElementChild.cloneNode(true);
      row.dataset.id = item.id;
      row.classList.toggle("done", item.done);
      row.querySelector(".todo-text").textContent = item.text;
      const toggle = row.querySelector('[data-action="toggle"]');
      toggle.setAttribute("aria-pressed", item.done ? "true" : "false");
      toggle.setAttribute("aria-label", item.done ? "标记未完成" : "标记完成");
      fragment.appendChild(row);
    });
    listEl.appendChild(fragment);
  }

  if (scrollToBottom) {
    scrollToBottom = false;
    requestAnimationFrame(() => {
      listEl.scrollTop = listEl.scrollHeight;
    });
  } else {
    listEl.scrollTop = previousScroll;
  }
}

function cancelUndo() {
  if (!undoSnapshot) return;
  undoSnapshot = null;
  undoCount = 0;
  window.clearTimeout(undoTimer);
  undoTimer = null;
}

function startUndoTimer() {
  window.clearTimeout(undoTimer);
  undoTimer = window.setTimeout(() => {
    undoSnapshot = null;
    undoCount = 0;
    undoTimer = null;
    updateAction(store.items);
  }, 6000);
}

function clearChecked() {
  const checked = countChecked(store.items);
  if (!checked) return;
  undoSnapshot = store.snapshot();
  undoCount = checked;
  haptic(12);
  store.clearDone();
  startUndoTimer();
}

function undoClear() {
  if (!undoSnapshot) return;
  const snapshot = undoSnapshot;
  cancelUndo();
  haptic(12);
  store.restore(snapshot);
}

store.subscribe(render);
render();

composer.addEventListener("submit", (event) => {
  event.preventDefault();
  const text = input.value.trim();
  if (!text) return;
  cancelUndo();
  scrollToBottom = true;
  const item = store.add(text);
  if (!item) {
    scrollToBottom = false;
    return;
  }
  input.value = "";
  haptic(8);
  input.focus({ preventScroll: true });
});

clearBtn.addEventListener("click", () => {
  if (undoSnapshot) undoClear();
  else clearChecked();
});

listEl.addEventListener("click", (event) => {
  const actionEl = event.target.closest("[data-action]");
  if (!actionEl) return;
  const row = actionEl.closest(".todo-row");
  if (!row) return;
  const id = row.dataset.id;
  haptic(8);
  cancelUndo();

  if (actionEl.dataset.action === "toggle") {
    store.toggle(id);
  } else if (actionEl.dataset.action === "delete") {
    haptic(12);
    store.remove(id);
  }
});

if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("./sw.js").catch(() => {});
  });
}
