const DEVICE_KEY = "monotask.device.v1";
const SCHEMA = 1;
const memory = new Map();

function storageGet(key) {
  try {
    return localStorage.getItem(key);
  } catch {
    return memory.has(key) ? memory.get(key) : null;
  }
}

function storageSet(key, value) {
  try {
    localStorage.setItem(key, value);
  } catch {
    memory.set(key, value);
  }
}

function uid() {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return "id-" + Date.now().toString(36) + "-" + Math.random().toString(36).slice(2, 10);
}

export function getDeviceId() {
  let value = storageGet(DEVICE_KEY);
  if (!value) {
    value = uid();
    storageSet(DEVICE_KEY, value);
  }
  return value;
}

function asNumber(value, fallback) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function normalizeItem(raw) {
  if (!raw || typeof raw !== "object") return null;
  const text = String(raw.text ?? "").trim();
  if (!text) return null;
  const createdAt = asNumber(raw.createdAt, Date.now());
  return {
    id: raw.id ? String(raw.id) : uid(),
    text: text.slice(0, 500),
    done: Boolean(raw.done),
    order: asNumber(raw.order, 0),
    createdAt,
    updatedAt: asNumber(raw.updatedAt, createdAt),
    deletedAt: raw.deletedAt ? asNumber(raw.deletedAt, 0) : null,
    updatedBy: raw.updatedBy ? String(raw.updatedBy) : null
  };
}

export function createListStore({ key, listId, maxItems = Infinity }) {
  const deviceId = getDeviceId();
  const listeners = new Set();

  function emptyState() {
    return { schema: SCHEMA, listId, updatedAt: 0, items: [] };
  }

  function normalizeState(raw) {
    if (!raw || typeof raw !== "object") return emptyState();
    const items = Array.isArray(raw.items)
      ? raw.items.map(normalizeItem).filter(Boolean)
      : [];
    items.sort((a, b) => a.order - b.order || a.createdAt - b.createdAt);
    return {
      schema: SCHEMA,
      listId,
      updatedAt: asNumber(raw.updatedAt, 0),
      items
    };
  }

  function load() {
    const raw = storageGet(key);
    if (!raw) return emptyState();
    try {
      return normalizeState(JSON.parse(raw));
    } catch {
      return emptyState();
    }
  }

  let state = load();

  function persist() {
    storageSet(key, JSON.stringify(state));
  }

  function notify() {
    listeners.forEach((listener) => {
      try {
        listener();
      } catch {}
    });
  }

  function commit() {
    persist();
    notify();
  }

  function activeItems() {
    return state.items.filter((item) => !item.deletedAt);
  }

  function sortedActive() {
    return activeItems().sort((a, b) => a.order - b.order || a.createdAt - b.createdAt);
  }

  function add(text) {
    if (activeItems().length >= maxItems) return null;
    const clean = String(text ?? "").trim().slice(0, 500);
    if (!clean) return null;
    const now = Date.now();
    const maxOrder = state.items.reduce((max, item) => Math.max(max, item.order), -1);
    const item = {
      id: uid(),
      text: clean,
      done: false,
      order: maxOrder + 1,
      createdAt: now,
      updatedAt: now,
      deletedAt: null,
      updatedBy: deviceId
    };
    state.items.push(item);
    state.updatedAt = now;
    commit();
    return item;
  }

  function toggle(id) {
    const item = state.items.find((entry) => entry.id === id && !entry.deletedAt);
    if (!item) return;
    const now = Date.now();
    item.done = !item.done;
    item.updatedAt = now;
    item.updatedBy = deviceId;
    state.updatedAt = now;
    commit();
  }

  function remove(id) {
    const item = state.items.find((entry) => entry.id === id && !entry.deletedAt);
    if (!item) return;
    const now = Date.now();
    item.deletedAt = now;
    item.updatedAt = now;
    item.updatedBy = deviceId;
    state.updatedAt = now;
    commit();
  }

  function reorder(ids) {
    const now = Date.now();
    let changed = false;
    ids.forEach((id, index) => {
      const item = state.items.find((entry) => entry.id === id && !entry.deletedAt);
      if (item && item.order !== index) {
        item.order = index;
        item.updatedAt = now;
        item.updatedBy = deviceId;
        changed = true;
      }
    });
    if (!changed) return;
    state.updatedAt = now;
    commit();
  }

  function clearDone() {
    const now = Date.now();
    let count = 0;
    state.items.forEach((item) => {
      if (!item.deletedAt && item.done) {
        item.deletedAt = now;
        item.updatedAt = now;
        item.updatedBy = deviceId;
        count += 1;
      }
    });
    if (!count) return 0;
    state.updatedAt = now;
    commit();
    return count;
  }

  function snapshot() {
    return JSON.parse(JSON.stringify(state));
  }

  function restore(nextState) {
    state = normalizeState(nextState);
    state.updatedAt = Date.now();
    commit();
  }

  function subscribe(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  }

  window.addEventListener("storage", (event) => {
    if (event.key !== key) return;
    state = load();
    notify();
  });

  return {
    key,
    listId,
    maxItems,
    get items() {
      return sortedActive();
    },
    count() {
      return activeItems().length;
    },
    add,
    toggle,
    remove,
    reorder,
    clearDone,
    snapshot,
    restore,
    subscribe
  };
}
