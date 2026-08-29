import { types, flow, Instance } from 'mobx-state-tree';
import { TimeEntryModel, TimeEntryModelType } from './timeEntryStore';
import * as api from '../utils/api';

const RootStore = types
    .model('RootStore', {
        timeEntries: types.array(TimeEntryModel),
        lastError: types.maybeNull(types.string),
        loading: types.optional(types.boolean, false),

        currentPage: types.optional(types.number, 1),
        pageSize: types.optional(types.number, 20),
    })
    .actions(self => {
        const normalize = (t: any): TimeEntryModelType => {
            try {
                return {
                    id: t.id ?? t._id ?? '',
                    employeeId: t.employeeId ?? t.employee ?? '',
                    projectId: t.projectId ?? t.project ?? '',
                    employeeFullName: t.employeeFullName ?? t.employeeId ?? 'Неизвестно',
                    projectCode: t.projectCode ?? t.projectId ?? 'Без проекта',
                    date: t.date ?? t.Date ?? '',
                    hours: (typeof t.hours === 'number')
                        ? t.hours
                        : (typeof t.hours === 'string' ? parseFloat(t.hours) : 0) || 0,
                    expectedCost: t.expectedCost == null ? undefined : Number(t.expectedCost),
                    comment: t.comment == null ? null : String(t.comment),
                    createdBy: t.createdBy == null ? null : String(t.createdBy),
                    createdAt: t.createdAt == null ? null : String(t.createdAt),
                    modifiedBy: t.modifiedBy == null ? null : String(t.modifiedBy),
                    modifiedAt: t.modifiedAt == null ? null : String(t.modifiedAt),
                    version: typeof t.version === 'number' ? t.version : Number(t.version ?? 1)
                } as TimeEntryModelType;
            } catch (e) {
                console.error('[store] normalize failed completely for item:', t, e);
                return {
                    id: 'unknown-' + Math.random(),
                    employeeId: '',
                    projectId: '',
                    employeeFullName: 'Ошибка данных',
                    projectCode: '',
                    date: '',
                    hours: 0,
                    expectedCost: undefined,
                    comment: null,
                    createdBy: null,
                    createdAt: null,
                    modifiedBy: null,
                    modifiedAt: null,
                    version: 1
                };
            }
        };

        const fetchTimeEntries = flow(function* (page: number = 1, pageSize: number = 20) {
            console.log('[store] fetchTimeEntries: start', { page, pageSize });

            self.currentPage = page;
            self.pageSize = pageSize;
            self.loading = true;

            try {
                const raw: any = yield api.fetchTimeEntries(page, pageSize);
                console.log('[store] fetchTimeEntries: api returned', raw);

                self.timeEntries.clear();

                let itemsToProcess: any[] = [];

                if (Array.isArray(raw)) {
                    itemsToProcess = raw;
                } else if (raw && typeof raw === 'object') {
                    if (Array.isArray(raw.Rows)) {
                        itemsToProcess = raw.Rows;
                    } else {
                        console.warn('[store] Unexpected response format', raw);
                    }
                }

                for (const item of itemsToProcess) {
                    try {
                        const norm = normalize(item);
                        self.timeEntries.push(norm);
                    } catch (innerErr) {
                        console.error('[store] normalize failed', item, innerErr);
                    }
                }

                console.log('[store] fetched items count:', self.timeEntries.length);
                self.lastError = null;
            } catch (err: any) {
                console.error('[store] fetchTimeEntries error', err);
                self.timeEntries.clear();
                self.lastError = err?.message ?? 'Ошибка при загрузке записей табеля';
            } finally {
                self.loading = false;
                console.log('[store] fetchTimeEntries finished');
            }
        });

        const addTimeEntry = flow(function* (payload: any) {
            try {
                console.log('[store] addTimeEntry', payload);
                yield api.createTimeEntry(payload);
                yield fetchTimeEntries(self.currentPage, self.pageSize);
            } catch (err: any) {
                console.error('[store] addTimeEntry error', err);
                self.lastError = err?.message ?? 'Ошибка при создании записи';
                throw err;
            }
        });

        const updateTimeEntry = flow(function* (id: string, payload: any) {
            try {
                console.log('[store] updateTimeEntry', id, payload);
                yield api.updateTimeEntry(id, payload);
                yield fetchTimeEntries(self.currentPage, self.pageSize);
            } catch (err: any) {
                console.error('[store] updateTimeEntry error', err);
                self.lastError = err?.message ?? 'Ошибка при обновлении записи';
                throw err;
            }
        });

        const deleteTimeEntry = flow(function* (id: string) {
            try {
                console.log('[store] deleteTimeEntry', id);
                yield api.deleteTimeEntry(id);
                yield fetchTimeEntries(self.currentPage, self.pageSize);
            } catch (err: any) {
                console.error('[store] deleteTimeEntry error', err);
                throw err;
            }
        });

        return { fetchTimeEntries, addTimeEntry, updateTimeEntry, deleteTimeEntry };
    });

export function createRootStore() {
    console.log('[store] createRootStore: creating store');
    const store = RootStore.create({
        timeEntries: [],
        lastError: null,
        loading: false,
        currentPage: 1,
        pageSize: 20
    });

    return store;
}

export type RootStore = Instance<typeof RootStore>;