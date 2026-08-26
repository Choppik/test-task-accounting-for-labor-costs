const API_BASE = 'http://localhost:5000';

async function fetchJson(url: string, init?: RequestInit) {
    const res = await fetch(url, init);
    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || res.statusText);
    }
    return res.json();
}

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