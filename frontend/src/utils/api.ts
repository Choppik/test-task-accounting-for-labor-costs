const API_BASE = 'http://localhost:5000';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
    let res: Response;

    try {
        res = await fetch(url, options);
    } catch (networkError) {
        throw new Error('Нет соединения с сервером. Проверьте интернет или доступность сервиса.');
    }

    if (!res.ok) {
        let userMessage = `Ошибка сервера: ${res.status}`;

        try {
            const text = await res.text();
            if (!text.trim()) throw new Error();

            const data = JSON.parse(text);

            if (data && typeof data.message === 'string') {
                userMessage = data.message;
            } else if (data && data.errors && typeof data.errors === 'object') {
                const allErrors = Object.values(data.errors).flat();
                if (allErrors.length > 0 && typeof allErrors[0] === 'string') {
                    userMessage = allErrors[0];
                }
            }
        } catch {
            // Если не JSON — оставляем дефолтное сообщение
        }

        throw new Error(userMessage);
    }

    // 2. Обработка успешного ответа
    // Если статус 204 (No Content) — тело пустое, JSON парсить нельзя!
    if (res.status === 204) {
        return undefined as unknown as T; // Или можно вернуть true, если T — boolean
    }

    return res.json() as T;
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


export async function getProjectReport(year: string, month: string) {
    const body = JSON.stringify({
        Year: year,
        Month: month,
        Page: 1,
        PageSize: 20,
    });

    return fetchJson(`${API_BASE}/api/reports/projects`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body,
    });
}