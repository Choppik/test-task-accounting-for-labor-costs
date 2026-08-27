import { types, flow, Instance } from 'mobx-state-tree';
import { TimeEntryModel, TimeEntryModelType } from './timeEntryStore';
import * as api from '../utils/api';

const RootStore = types
    .model('RootStore', {
        timeEntries: types.array(TimeEntryModel),
        lastError: types.maybeNull(types.string),
        loading: types.optional(types.boolean, false)
    })
    .actions(self => {
        const normalize = (t: any): TimeEntryModelType => {
            return {
                id: t.id ?? t._id ?? '',
                employeeId: t.employeeId ?? t.employee ?? '',
                projectId: t.projectId ?? t.project ?? '',
                employeeFullName: t.employeeFullName ?? t.employeeId ?? '',
                projectCode: t.projectCode ?? t.projectId,
                date: t.date ?? t.Date ?? '',
                hours: typeof t.hours === 'number' ? t.hours : Number(t.hours ?? 0),
                expectedCost: t.expectedCost == null ? undefined : Number(t.expectedCost),
                comment: t.comment == null ? null : String(t.comment),
                createdBy: t.createdBy == null ? null : String(t.createdBy),
                createdAt: t.createdAt == null ? null : String(t.createdAt),
                modifiedBy: t.modifiedBy == null ? null : String(t.modifiedBy),
                modifiedAt: t.modifiedAt == null ? null : String(t.modifiedAt),
                version: typeof t.version === 'number' ? t.version : Number(t.version ?? 1)
            } as TimeEntryModelType;
        };

        const fetchTimeEntries = flow(function* () {
            console.log('[store] fetchTimeEntries: start');
            self.loading = true;
            try {
                const raw: any = yield api.fetchTimeEntries();
                console.log('[store] fetchTimeEntries: api returned', Array.isArray(raw) ? raw.length : raw);
                self.timeEntries.clear();

                if (Array.isArray(raw)) {
                    for (const item of raw) {
                        try {
                            const norm = normalize(item);
                            self.timeEntries.push(norm);
                        } catch (innerErr) {
                            console.error('[store] fetchTimeEntries: normalize/push failed for item', item, innerErr);
                        }
                    }
                } else {
                    console.warn('[store] fetchTimeEntries: api returned non-array', raw);
                }

                console.log('[store] fetchTimeEntries: timesheets length after push', self.timeEntries.length);
                self.lastError = null;
            } catch (err: any) {
                console.error('[store] fetchTimeEntries: error', err);
                self.timeEntries.clear();
                self.lastError = err?.message ?? 'Ошибка при загрузке записей табеля';
            } finally {
                self.loading = false;
                console.log('[store] fetchTimeEntries: finished, loading=false');
            }
        });

        const addTimeEntry = flow(function* (payload: any) {
            try {
                console.log('[store] addTimeEntry payload', payload);
                const res = yield api.createTimeEntry(payload);
                yield fetchTimeEntries();
                return res;
            } catch (err: any) {
                console.error('[store] addTimeEntry error', err);
                self.lastError = err?.message ?? 'Ошибка при создании записи';
                throw err;
            }
        });

        const updateTimeEntry = flow(function* (id: string, payload: any) {
            try {
                console.log('[store] updateTimeEntry', id, payload);
                const res = yield api.updateTimeEntry(id, payload);
                yield fetchTimeEntries();
                return res;
            } catch (err: any) {
                console.error('[store] updateTimeEntry error', err);
                self.lastError = err?.message ?? 'Ошибка при обновлении записи';
                throw err;
            }
        });

        const deleteTimeEntry = flow(function* (id: string) {
            try {
                yield api.deleteTimeEntry(id); // fetchJson теперь вернёт null при 204
                if (fetchTimeEntries) yield fetchTimeEntries();
                return true;
            } catch (err) {
                console.error('[store] deleteTimeEntry error', err);
                throw err;
            }
        });

        return { fetchTimeEntries, addTimeEntry, updateTimeEntry, deleteTimeEntry };
    });

export function createRootStore() {
    console.log('[store] createRootStore: creating store');
    const store = RootStore.create({ timeEntries: [] as any, lastError: null, loading: false });
    try {
        (store as any).fetchTimeEntries();
        console.log('[store] createRootStore: fetchTimeEntries called');
    } catch (err) {
        console.error('[store] createRootStore: failed to call fetchTimeEntries', err);
    }
    return store;
}

export type RootStore = Instance<typeof RootStore>;