import React, { useState, useEffect, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';

import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-alpine.css';

import { Button, Intent, Classes } from '@blueprintjs/core';
import * as api from '../utils/api';
import { useStore } from '../stores/useStore';

type Props = { onClose?: () => void };

const ReportProject: React.FC<Props> = ({ onClose }) => {
    const store = useStore();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const currentYear = new Date().getFullYear();
    const currentMonth = String(new Date().getMonth() + 1).padStart(2, '0');

    const [year, setYear] = useState<string>(String(currentYear));
    const [month, setMonth] = useState<string>(currentMonth);

    const [rows, setRows] = useState<any[]>([]);

    const loadReport = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await api.getProjectReport(year, month);
            setRows(data);
        } catch (err: any) {
            console.error('Report load failed', err);
            if (err.status === 400 && err.data?.errors) {
                const errors = err.data.errors;
                const message = Object.values(errors).flat().join('; ');
                setError(`Ошибка параметров: ${message}`);
            } else {
                setError(err?.message ?? 'Не удалось загрузить отчёт');
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadReport();
    }, []);

    const columnDefs = useMemo(() => [
        { headerName: 'Проект', field: 'projectName', flex: 2 },
        { headerName: 'Код', field: 'projectCode', width: 100 },
        {
            headerName: 'Бюджет', field: 'budgetRub', width: 120,
            valueFormatter: (p: any) => p.value ? new Intl.NumberFormat('ru-RU').format(p.value) : '0'
        },
        { headerName: 'Факт (часы)', field: 'totalHours', width: 120 },
        {
            headerName: 'Факт (стоимость)', field: 'totalCost', width: 140,
            valueFormatter: (p: any) => p.value ? new Intl.NumberFormat('ru-RU').format(p.value) : '0'
        },
        {
            headerName: 'Освоено, %',
            field: 'percentSpent',
            width: 100,
            valueFormatter: (params: any) => {
                if (params.value === undefined || params.value === null) return '-';
                return `${params.value.toFixed(1)}%`;
            },
            cellStyle: (params: any) => {
                const percent = Number(params.value);
                // Если перерасход — красный фон, иначе белый/нейтральный
                return percent > 100 ? { backgroundColor: '#ffebee', color: '#c62828' } : null;
            }
        },
        {
            headerName: 'Начало', field: 'startDate', width: 110,
            valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : ''
        },
        {
            headerName: 'Конец', field: 'endDate', width: 110,
            valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : ''
        },
    ], []);

    // Простая валидация прямо в обработчике — можно вынести в Yup, если нужно
    const validateYear = (val: string) => /^\d{4}$/.test(val);
    const validateMonth = (val: string) => /^(0[1-9]|1[0-2])$/.test(val);

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
                            // Автодополнение до двух знаков при вводе одной цифры
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
                    ⚠️ {error}
                </div>
            )}

            {loading ? (
                <div style={{ textAlign: 'center', padding: 40, color: '#5c7080' }}>Загрузка отчёта...</div>
            ) : (
                <div style={{ height: 500, width: '100%' }}>
                    <AgGridReact
                        rowData={rows}
                        columnDefs={columnDefs}
                        defaultColDef={{ resizable: true, sortable: true, filter: true }}
                        pagination
                        paginationPageSize={25}
                    />
                </div>
            )}
        </div>
    );
};

export default ReportProject;