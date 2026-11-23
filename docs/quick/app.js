import { createListStore } from "../shared/store.js";
import { setupInstall, haptic, isNativeShell } from "../shared/ui.js";

const MAX_ITEMS = 5;
const store = createListStore({
  key: "monotask.quick.v1",
  listId: "quick",
  maxItems: MAX_ITEMS
});

const listEl = document.getElementById("list");
const countEl = document.getElementById("count");
const composer = document.getElementById("composer");
const input = document.getElementById("input");
const addBtn = document.getElementById("addBtn");
const installBtn = document.getElementById("installBtn");
const rowTemplate = document.getElementById("rowTemplate");

let draggingRow = null;
let dragPointerId = null;
let dragStartY = 0;
let dragActive = false;
let pendingRender = false;
let suppressClickUntil = 0;

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
    <p class="empty-title">清单是空的</p>
    <p class="empty-hint">添加最多 5 项待办，拖动左侧把手即可调整优先级。</p>
  `;
  return element;
}

function updateComposer(count) {
  const full = count >= MAX_ITEMS;
  input.disabled = full;
  addBtn.disabled = full;
  input.placeholder = full ? "已达 5 项上限" : "添加待办，回车确认";
}

function render() {
  if (dragActive) {
    pendingRender = true;
    return;
  }

  const items = store.items;
  countEl.textContent = items.length + " / " + MAX_ITEMS;
  updateComposer(items.length);
  listEl.textContent = "";

  if (!items.length) {
    listEl.appendChild(createEmptyState());
    return;
  }

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

store.subscribe(render);
render();

composer.addEventListener("submit", (event) => {
  event.preventDefault();
  if (input.disabled) return;
  const text = input.value.trim();
  if (!text) return;
  const item = store.add(text);
  if (!item) {
    haptic(16);
    return;
  }
  input.value = "";
  haptic(8);
  input.focus({ preventScroll: true });
});

listEl.addEventListener("click", (event) => {
  if (Date.now() < suppressClickUntil) {
    event.preventDefault();
    event.stopPropagation();
    return;
  }
  const actionEl = event.target.closest("[data-action]");
  if (!actionEl) return;
  const row = actionEl.closest(".todo-row");
  if (!row) return;
  const id = row.dataset.id;
  haptic(8);

  if (actionEl.dataset.action === "toggle") {
    store.toggle(id);
  } else if (actionEl.dataset.action === "delete") {
    haptic(12);
    store.remove(id);
  }
});

listEl.addEventListener("pointerdown", (event) => {
  if (event.pointerType === "mouse" && event.button !== 0) return;
  if (!event.target.closest(".grip")) return;
  const row = event.target.closest(".todo-row");
  if (!row || store.items.length < 2) return;
  if (document.activeElement === input) input.blur();

  draggingRow = row;
  dragPointerId = event.pointerId;
  dragStartY = event.clientY;
  dragActive = false;
  pendingRender = false;
  try {
    row.setPointerCapture(event.pointerId);
  } catch {}
});

listEl.addEventListener(
  "pointermove",
  (event) => {
    if (!draggingRow || event.pointerId !== dragPointerId) return;

    if (!dragActive) {
      if (Math.abs(event.clientY - dragStartY) < 6) return;
      dragActive = true;
      draggingRow.classList.add("dragging");
      listEl.classList.add("dragging-active");
      haptic(8);
    }

    event.preventDefault();
    const otherRows = Array.from(listEl.querySelectorAll(".todo-row")).filter(
      (row) => row !== draggingRow
    );
    let target = 0;
    for (const row of otherRows) {
      const rect = row.getBoundingClientRect();
      if (event.clientY > rect.top + rect.height / 2) target += 1;
    }

    const reference = otherRows[target] || null;
    if (reference) {
      if (draggingRow.nextElementSibling !== reference) {
        listEl.insertBefore(draggingRow, reference);
      }
    } else if (listEl.lastElementChild !== draggingRow) {
      listEl.appendChild(draggingRow);
    }
  },
  { passive: false }
);

function finishDrag(event, cancelled) {
  if (!draggingRow || (event && event.pointerId !== dragPointerId)) return;

  const row = draggingRow;
  const wasActive = dragActive;
  const pointerId = dragPointerId;

  row.classList.remove("dragging");
  listEl.classList.remove("dragging-active");
  draggingRow = null;
  dragPointerId = null;
  dragActive = false;

  try {
    row.releasePointerCapture(pointerId);
  } catch {}

  if (wasActive) {
    suppressClickUntil = Date.now() + 300;
    haptic(12);
    if (cancelled) {
      render();
    } else {
      const ids = Array.from(listEl.querySelectorAll(".todo-row")).map(
        (item) => item.dataset.id
      );
      store.reorder(ids);
    }
  }

  if (pendingRender) {
    pendingRender = false;
    render();
  }
}

listEl.addEventListener("pointerup", (event) => finishDrag(event, false));
listEl.addEventListener("pointercancel", (event) => finishDrag(event, true));
listEl.addEventListener("contextmenu", (event) => {
  if (draggingRow || event.target.closest(".grip")) event.preventDefault();
});

if ("serviceWorker" in navigator && !isNativeShell()) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("./sw.js").catch(() => {});
  });
}
