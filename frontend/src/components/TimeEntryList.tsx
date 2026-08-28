import React, { useEffect, useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button } from '@blueprintjs/core';
import * as api from '../utils/api';
import { useStore } from '../stores/useStore';
import TimeEntry from './TimeEntry';
import { getSnapshot } from 'mobx-state-tree';

type Props = { onClose?: () => void };

const TimeEntryList: React.FC<Props> = ({ onClose }) => {
    const store = useStore();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [employees, setEmployees] = useState<any[]>([]);
    const [projects, setProjects] = useState<any[]>([]);

    const [selected, setSelected] = useState<any | null>(null);
    const [modalOpen, setModalOpen] = useState(false);

    const loadAll = async () => {
        setLoading(true);
        setError(null);
        try {
            if (store.fetchTimeEntries) await store.fetchTimeEntries();
            const emps = await api.fetchEmployees();
            setEmployees(Array.isArray(emps) ? (emps) : []);
            const projs = await api.fetchProjects();
            setProjects(Array.isArray(projs) ? (projs) : []);
        } catch (e: any) {
            console.error(e);
            setError(e?.message ?? 'Ошибка загрузки');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadAll(); }, []);

    const rows = (store.timeEntries ?? []).map((node: any) => {
        try {
            return getSnapshot(node);
        } catch {
            return node;
        }
    });

    const onCellClicked = (event: any) => {
        const field = event.colDef && (event.colDef.field || event.colDef.colId);
        if (field === 'act' || field === 'actions') {
            return;
        }

        let plain = event.data;
        try { 
            const { getSnapshot } = require('mobx-state-tree');
            plain = getSnapshot(event.data);
        } catch { /* noop */ }
        setSelected(plain);
        setModalOpen(true);
    };

    const columnDefs = useMemo(() => [
        { headerName: 'ID', field: 'id', hide: true },
        { headerName: 'Сотрудник', field: 'employeeFullName', flex: 1 },
        { headerName: 'Проект', field: 'projectCode', flex: 1 },
        { headerName: 'Дата', field: 'date', width: 140, valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : '' },
        { headerName: 'Часы', field: 'hours', width: 100 },
        { headerName: 'Ожидаемая стоимость', field: 'expectedCost', width: 100 },
        { headerName: 'Комментарий', field: 'comment', flex: 2 },
        { headerName: 'Версия', field: 'version', width: 90 },
        {
            headerName: '', field: 'act', width: 100,
            cellRendererFramework: (params: any) => {
                const handleClick = async (ev: React.MouseEvent) => {

                    ev.stopPropagation();

                    if ((ev as any).nativeEvent && (ev as any).nativeEvent.stopImmediatePropagation) {
                        (ev as any).nativeEvent.stopImmediatePropagation();
                    }

                    try {
                        setLoading(true);
                        if (store.deleteTimeEntry) {
                            await store.deleteTimeEntry(params.data.id);
                        } else {
                            await api.deleteTimeEntry(params.data.id);
                            if (store.fetchTimeEntries) await store.fetchTimeEntries();
                        }
                    } catch (err: any) {
                        console.error('Delete failed', err);
                        alert('Ошибка удаления: ' + (err?.message ?? ''));
                    } finally {
                        setLoading(false);
                    }
                };

                return (
                    <button className="bp3-button bp3-minimal bp3-intent-danger"
                        onClick={handleClick}
                        onMouseDown={(e) => e.stopPropagation()}
                        onDoubleClick={(e) => e.stopPropagation()}>
                        X
                    </button>
                );
            }
        }
    ], [store]);

    const onSaved = async () => {
        await loadAll();
    };

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <Button minimal onClick={() => loadAll()}>Обновить</Button>
                <Button intent="primary" onClick={() => { setSelected(null); setModalOpen(true); }}>Добавить запись</Button>
                <Button onClick={() => onClose?.()}>Закрыть</Button>
            </div>

            {loading && <div>Загрузка...</div>}
            {error && <div style={{ color: 'red' }}>{error}</div>}

            <div className="ag-theme-alpine" style={{ height: 480, width: '100%' }}>
                <AgGridReact
                    rowData={rows}
                    columnDefs={columnDefs}
                    defaultColDef={{ resizable: true, sortable: true, filter: true }}
                    pagination
                    paginationPageSize={20}
                    onCellClicked={onCellClicked}
                />
            </div>

            <TimeEntry
                isOpen={modalOpen}
                onClose={() => setModalOpen(false)}
                onSaved={onSaved}
                initial={selected}
                employees={employees}
                projects={projects}
                store={store}
            />
        </div>
    );
};

export default TimeEntryList;