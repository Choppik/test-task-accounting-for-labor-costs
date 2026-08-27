const API_BASE = 'http://localhost:5000';

async function fetchJson(url: string, init?: RequestInit) {
    const res = await fetch(url, init);
    // если нет контента — вернуть null (или undefined)
    if (res.status === 204) return null;

    const text = await res.text();
    if (!text) return null; // пустой ответ — безопасно возвращаем null

    try {
        return JSON.parse(text);
    } catch (err) {
        // если не JSON — пробуем вернуть raw text или бросаем
        throw new Error('Invalid JSON response: ' + (err as Error).message);
    }
}

export { fetchJson, API_BASE };

export function fetchTimeEntries() {
    return fetchJson(`${API_BASE}/api/timeentry`);
}

export const fetchTimeEntriesFallback = fetchTimeEntries;

export function createTimeEntry(payload: any) {
    return fetchJson(`${API_BASE}/api/timeentry`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
}

export function updateTimeEntry(id: string, payload: any) {
    return fetchJson(`${API_BASE}/api/timeentry/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
}

export function deleteTimeEntry(id: string) {
    return fetchJson(`${API_BASE}/api/timeentry/${id}`, {
        method: 'DELETE'
    });
}

export function fetchEmployees() {
    return fetchJson(`${API_BASE}/api/employees`);
}

export function fetchProjects() {
    return fetchJson(`${API_BASE}/api/projects`);
}