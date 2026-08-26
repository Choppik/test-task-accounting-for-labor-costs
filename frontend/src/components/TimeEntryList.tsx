import React, { useEffect, useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button } from '@blueprintjs/core';
import * as api from '../utils/api';
import { useStore } from '../stores/useStore';

type Props = {
    onClose?: () => void;
};

const TimeEntrytList: React.FC<Props> = ({ onClose }) => {
    const store = useStore();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                if ((store as any).fetchTimeEntries) {
                    console.log('[TimesheetList] calling store.fetchTimeEntries()');
                    await (store as any).fetchTimeEntries();
                    console.log('[TimesheetList] after store.fetchTimeEntries(), store.length=', (store as any).timesheets.length);
                } else {
                    console.log('[TimesheetList] store.fetchTimeEntries not found, calling API directly');
                    const data = await api.fetchTimeEntries();
                    console.log('[TimesheetList] api returned', data && data.length);
                    (store as any).timesheets.clear();
                    data.forEach((t: any) => (store as any).timesheets.push({
                        id: t.id ?? t._id ?? '',
                        employeeId: t.employeeId ?? '',
                        projectId: t.projectId ?? '',
                        date: t.date ?? '',
                        hours: t.hours ?? 0,
                        comment: t.comment ?? null,
                        createdBy: t.createdBy ?? null,
                        createdAt: t.createdAt ?? null,
                        modifiedBy: t.modifiedBy ?? null,
                        modifiedAt: t.modifiedAt ?? null,
                        version: t.version ?? 1
                    }));
                }
            } catch (e: any) {
                console.error('[TimesheetList] load error', e);
                setError(e?.message ?? 'Ошибка при загрузке записей');
            } finally {
                if (mounted) setLoading(false);
            }
        })();
        return () => { mounted = false; };
    }, [store]);

    const rows = (store as any).timeEntries ? (store as any).timeEntries.slice() : [];
    console.log('TimesheetList rows', rows);
    const columnDefs = useMemo(() => [
        { headerName: 'ID', field: 'id', hide: true },
        { headerName: 'Сотрудник', field: 'employeeId', sortable: true, filter: true, flex: 1 },
        { headerName: 'Проект', field: 'projectId', sortable: true, filter: true, flex: 1 },
        {
            headerName: 'Дата', field: 'date', sortable: true, filter: true, width: 140,
            valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : ''
        },
        { headerName: 'Часы', field: 'hours', sortable: true, filter: true, width: 100 },
        { headerName: 'Комментарий', field: 'comment', flex: 2 },
        { headerName: 'Создал', field: 'createdBy', width: 140 },
        { headerName: 'Версия', field: 'version', width: 100 }
    ], []);

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <Button minimal onClick={async () => { setLoading(true); try { await (store as any).fetchTimeEntries(); } finally { setLoading(false); } }}>Обновить</Button>
                <Button intent="primary" onClick={() => onClose?.()}>Закрыть</Button>
            </div>

            {loading && <div>Загрузка...</div>}
            {error && <div style={{ color: 'red' }}>{error}</div>}

            {!loading && !error && (
                <div className="ag-theme-alpine" style={{ height: 480, width: '100%' }}>
                    <AgGridReact
                        rowData={rows}
                        columnDefs={columnDefs}
                        pagination
                        paginationPageSize={25}
                        defaultColDef={{ resizable: true }}
                    />
                </div>
            )}
        </div>
    );
};

export default TimeEntrytList;