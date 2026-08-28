import React, { useEffect, useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button } from '@blueprintjs/core';
import * as api from '../utils/api';
import { useStore } from '../stores/useStore';
import TimeEntry from './TimeEntry';

type Props = { onClose?: () => void };

const TimeEntryList: React.FC<Props> = ({ onClose }) => {
    const store = useStore();

    // Локальные стейты для полей пагинации
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);

    const [employees, setEmployees] = useState<any[]>([]);
    const [projects, setProjects] = useState<any[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [selected, setSelected] = useState<any | null>(null);
    const [modalOpen, setModalOpen] = useState(false);

    const loadAll = async () => {
        try {
            await store.fetchTimeEntries(page, pageSize);

            const emps = await api.fetchEmployees();
            setEmployees(Array.isArray(emps) ? emps : []);

            const projs = await api.fetchProjects();
            setProjects(Array.isArray(projs) ? projs : []);
        } catch (e: any) {
            console.error(e);
            if (!store.lastError) {
                setError('Не удалось загрузить список записей. Проверьте соединение.');
            }
        }
    };

    useEffect(() => {
        loadAll();
    }, []);

    const rows = store.timeEntries; 

    const onCellClicked = (event: any) => {
        const field = event.colDef && (event.colDef.field || event.colDef.colId);
        if (field === 'act' || field === 'actions') {
            return;
        }

        let plain = event.data;
        console.log(plain);
        setSelected(plain);
        setModalOpen(true);
    };

    const columnDefs = useMemo(() => [
        { headerName: 'ID', field: 'id', hide: true },
        { headerName: 'Сотрудник', field: 'employeeFullName', flex: 1 },
        { headerName: 'Проект', field: 'projectCode', flex: 1 },
        {
            headerName: 'Дата', field: 'date', width: 140,
            valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : ''
        },
        { headerName: 'Часы', field: 'hours', width: 100 },
        { headerName: 'Ожидаемая стоимость', field: 'expectedCost', width: 100 },
        { headerName: 'Комментарий', field: 'comment', flex: 2 },
        { headerName: 'Версия', field: 'version', width: 90 },
        {
            headerName: '',
            field: 'act',
            width: 100,
            cellRendererFramework: (params: any) => {
                const id = params.data?.id;
                if (id == null) {
                    return null;
                }

                const handleClick = async (ev: React.MouseEvent) => {
                    ev.stopPropagation();
                    if ((ev as any).nativeEvent?.stopImmediatePropagation) {
                        (ev as any).nativeEvent.stopImmediatePropagation();
                    }

                    try {
                        if (store.deleteTimeEntry) {
                            await store.deleteTimeEntry(id);
                        } else {
                            await api.deleteTimeEntry(id);
                            if (store.fetchTimeEntries) {
                                await store.fetchTimeEntries(page, pageSize);
                            }
                        }

                        if (params.api) {
                            params.api.setRowData(store.timeEntries);
                        }
                        setError(null);
                    } catch (err: any) {
                        console.error('Delete failed', err);
                        const msg = err?.message || 'Произошла неизвестная ошибка при удалении';
                        setError(msg);
                    }
                };

                return (
                    <button
                        className="bp3-button bp3-minimal bp3-intent-danger"
                        onClick={handleClick}
                        onMouseDown={(e) => e.stopPropagation()}
                        onDoubleClick={(e) => e.stopPropagation()}>
                        X
                    </button>
                );
            }
        }
    ], [store, page, pageSize]);

    const onSaved = async () => {
        await loadAll();
    };

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, height: '100%', minHeight: '500px' }}>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <Button minimal onClick={() => { setError(null), loadAll() }}>Обновить</Button>
                <Button intent="primary" onClick={() => { setError(null), setSelected(null); setModalOpen(true); }}>Добавить запись</Button>
            </div>

            {store.loading && <div style={{ padding: 20, textAlign: 'center' }}>Загрузка данных...</div>}
            {store.lastError && <div style={{ color: 'red', padding: 10 }}>{store.lastError}</div>}

            {error && (
                <div style={{
                    padding: 12,
                    backgroundColor: '#ffebee',
                    color: '#c62828',
                    borderRadius: 4,
                    marginBottom: 16
                }}>
                    {error}
                </div>
            )}
            <div
                className="ag-theme-alpine"
                style={{ height: '480px', width: '100%', border: '1px solid #ddd' }}
            >
                <AgGridReact
                    key={rows.length}
                    columnDefs={columnDefs}
                    defaultColDef={{ resizable: true, sortable: true, filter: true }}
                    onCellClicked={onCellClicked}
                    gridOptions={{
                        suppressRowClickSelection: true,
                        rowSelection: 'none',
                        onGridReady: (params) => { params.api.setRowData(rows); },
                        onFirstDataRendered: (params) => {
                            const count = params.api.getDisplayedRowCount();
                            if (count === 0) {
                                console.error('CRITICAL: Grid rendered but has 0 rows. Check store.timeEntries content.');
                                console.log('Debug store.timeEntries:', store.timeEntries);
                            }
                        }
                    }}
                />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 16, paddingTop: 8 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <label style={{ margin: 0, fontSize: 13 }}>Страница:</label>
                    <input type="number" value={page} onChange={(e) => setPage(Math.max(1, Number(e.target.value) || 1))} style={{ width: 60 }} />
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <label style={{ margin: 0, fontSize: 13 }}>Размер:</label>
                    <input type="number" value={pageSize} onChange={(e) => setPageSize(Math.max(1, Math.min(100, Number(e.target.value) || 20)))} style={{ width: 60 }} />
                </div>
                <Button intent="success" onClick={loadAll}>Загрузить</Button>
                <Button onClick={() => onClose?.()}>Закрыть</Button>
            </div>

            <TimeEntry isOpen={modalOpen} onClose={() => setModalOpen(false)} onSaved={onSaved} initial={selected} employees={employees} projects={projects} store={store} />
        </div>
    );
};

export default TimeEntryList;