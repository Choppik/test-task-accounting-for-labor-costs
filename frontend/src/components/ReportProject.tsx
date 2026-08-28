import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button, Intent, Classes } from '@blueprintjs/core';
import * as api from '../utils/api';

type Props = { onClose?: () => void };

const ReportProject: React.FC<Props> = ({ onClose }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const currentYear = new Date().getFullYear();
    const currentMonth = String(new Date().getMonth() + 1).padStart(2, '0');

    const [year, setYear] = useState<string>(String(currentYear));
    const [month, setMonth] = useState<string>(currentMonth);

    const [rows, setRows] = useState<any[]>([]);

    const [totals, setTotals] = useState({ totalHours: 0, totalCost: 0 });

    const recalculateTotals = useCallback((data: any[]) => {
        let totalHours = 0;
        let totalCost = 0;

        data.forEach((row) => {
            const hours = Number(row.totalHours ?? 0);
            const cost = Number(row.totalCost ?? 0);

            totalHours += hours;
            totalCost += cost;
        });

        setTotals({ totalHours, totalCost });
    }, []);

    const onFilterChanged = useCallback((params: any) => {
        const api = params.api;
        const visibleRows: any[] = [];

        for (let i = 0; i < api.getDisplayedRowCount(); i++) {
            const rowNode = api.getDisplayedRowAtIndex(i);

            if (!rowNode) continue;

            if (!rowNode.group) {
                visibleRows.push(rowNode.data);
            }
        }

        recalculateTotals(visibleRows);
    }, [recalculateTotals]);

    const gridApiRef = React.useRef<any>(null);

    const calculateVisibleTotals = useCallback(() => {
        if (!gridApiRef.current) return;
        const api = gridApiRef.current;
        const visibleRows: any[] = [];
        for (let i = 0; i < api.getDisplayedRowCount(); i++) {
            const node = api.getDisplayedRowAtIndex(i);
            if (!node || node.group) continue;
            visibleRows.push(node.data);
        }
        recalculateTotals(visibleRows);
    }, [recalculateTotals]);

    const onGridReady = useCallback((params: any) => {
        gridApiRef.current = params.api;
        calculateVisibleTotals();
    }, [calculateVisibleTotals]);


    const loadReport = async () => {
        setLoading(true);
        setError(null);

        try {
            const data = await api.getProjectReport(year, month);
            setRows(data as any[]);
        }
            catch (err: unknown) {
                let userMessage = 'Не удалось загрузить отчёт.';

                if (err instanceof Error) {
                    if (err.message.includes('Failed to fetch')) {
                        userMessage = 'Нет соединения с сервером. Проверьте интернет.';
                    }
                    else {
                        userMessage = err.message;
                    }
                }
                setError(userMessage);
            } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadReport();
    }, []);

    const getRowStyle = useMemo(() => (params: any) => {
        if (params.data?.projectCode === 'Итого') {
            return {
                fontWeight: 'bold',
                borderTop: '2px solid #ccc',
            };
        }
        return undefined;
    }, []);

    const columnDefs = useMemo(() => [
        {
            headerName: 'Проект',
            field: 'projectCode',
            flex: 2,
        },
        {
            headerName: 'Часы',
            field: 'totalHours',
            width: 100,
            valueFormatter: (params: { value: number | null | undefined }) =>
                params.value ? params.value.toString() : '0'
        },
        {
            headerName: 'Стоимость, руб',
            field: 'totalCost',
            width: 150,
            valueFormatter: (params: { value: number | null | undefined }) => {
                if (params.value === undefined || params.value === null) return '0';
                return new Intl.NumberFormat('ru-RU').format(params.value);
            },
        },
        {
            headerName: 'Бюджет, руб',
            field: 'budgetRub',
            width: 150,
            valueFormatter: (params: { value: number | null | undefined }) => {
                if (params.value === undefined || params.value === null) return '';
                return `${new Intl.NumberFormat('ru-RU').format(params.value)}`;
            },
        },
        {
            headerName: 'Освоено, %',
            field: 'percentSpent',
            width: 150,
            valueFormatter: (params: { value: number | null | undefined }) => {
                if (params.value === undefined || params.value === null) return '';
                return `${params.value.toFixed(1)}%`;
            },
            cellStyle: (params: any) => {
                const percent = Number(params.value);
                if (!Number.isFinite(percent)) return null;

                if (percent > 100) {
                    return { backgroundColor: '#ffebee', color: '#c62828' }; // Красный
                }
                if (percent >= 80 && percent <= 100) {
                    return { backgroundColor: '#fff3e0', color: '#ef6c00' }; // Оранжевый
                }
                return null; // Нейтральный
            },
        },
    ], []);

    const validateYear = (val: string) => /^\d{4}$/.test(val);
    const validateMonth = (val: string) => /^(0[1-9]|1[0-2])$/.test(val);


    // Формируем строку итогов
    const footerRow = useMemo(() => [{
        projectCode: 'Итого',
        totalHours: totals.totalHours,
        totalCost: totals.totalCost,
        budgetRub: null,
        percentSpent: null, 
    }], [totals]);

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16, padding: 16, maxHeight: '80vh', overflow: 'hidden' }}>
            <div style={{ display: 'flex', gap: 16, alignItems: 'flex-end' }}>
                <div style={{ flex: 1 }}>
                    <label style={{ display: 'block', marginBottom: 4, fontSize: 12, color: '#5c7080' }}>Год (4 цифры)</label>
                    <input
                        type="text"
                        value={year}
                        onChange={(e) => setYear(e.target.value)}
                        placeholder="2024"
                        maxLength={4}
                        className={Classes.INPUT}
                        style={{ width: '100%' }}
                    />
                </div>

                <div style={{ width: 150 }}>
                    <label style={{ display: 'block', marginBottom: 4, fontSize: 12, color: '#5c7080' }}>Месяц (01–12)</label>
                    <input
                        type="text"
                        value={month}
                        onChange={(e) => {
                            const val = e.target.value.replace(/\D/g, '').slice(0, 2);
                            setMonth(val.length === 1 ? `0${val}` : val);
                        }}
                        placeholder="03"
                        maxLength={2}
                        className={Classes.INPUT}
                        style={{ width: '100%' }}
                    />
                </div>

                <Button
                    intent={Intent.PRIMARY}
                    onClick={loadReport}
                    loading={loading}
                    disabled={!validateYear(year) || !validateMonth(month)}
                >
                    Показать отчёт
                </Button>

                <Button minimal onClick={onClose}>
                    Закрыть
                </Button>
            </div>

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

            {loading ? (
                <div style={{ textAlign: 'center', padding: 40, color: '#5c7080' }}>Загрузка отчёта...</div>
            ) : (
                <div className="ag-theme-alpine" style={{ height: 500, width: '100%' }}>
                    <AgGridReact
                        rowData={rows}
                        columnDefs={columnDefs}
                        defaultColDef={{ resizable: true, sortable: true, filter: true }}
                        enableRangeSelection={true}
                        pagination={false}
                        pinnedBottomRowData={footerRow}
                        getRowStyle={getRowStyle as unknown as (params: any) => any}
                        onFilterChanged={onFilterChanged}
                        onGridReady={onGridReady}
                    />
                </div>
            )}
        </div>
    );
};

export default ReportProject;