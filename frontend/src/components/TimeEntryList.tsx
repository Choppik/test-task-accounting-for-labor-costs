import React, { useEffect, useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button, HTMLSelect, InputGroup, FormGroup, Intent } from '@blueprintjs/core';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import * as Yup from 'yup';
import * as api from '../utils/api';
import { useStore } from '../stores/useStore';

type Props = {
    onClose?: () => void;
};

const EntrySchema = Yup.object().shape({
    employeeId: Yup.string().required('Сотрудник обязателен'),
    projectId: Yup.string().required('Проект обязателен'),
    date: Yup.string().required('Дата обязательна'),
    hours: Yup.number().required('Часы обязательны').min(0.5, 'Минимум 0.5').max(24, 'Максимум 24')
});

const TimeEntryList: React.FC<Props> = ({ onClose }) => {
    const store = useStore();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [employees, setEmployees] = useState<any[]>([]);
    const [projects, setProjects] = useState<any[]>([]);

    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {

                if ((store as any).fetchTimeEntries) {
                    await (store as any).fetchTimeEntries();
                } else {
                    const data = await api.fetchTimeEntries();
                    if (Array.isArray(data) && (store as any).timeEntries) {
                        (store as any).timeEntries.clear();
                        data.forEach((t: any) => (store as any).timeEntries.push({
                            id: t.id ?? t._id ?? '',
                            employeeFullName: t.employeeFullName ?? null,
                            projectCode: t.projectCode ?? null,
                            employeeId: t.employeeId ?? '',
                            projectId: t.projectId ?? '',
                            date: t.date ?? '',
                            hours: t.hours ?? 0,
                            expectedCost: t.expectedCost ?? 0,
                            comment: t.comment ?? null,
                            createdBy: t.createdBy ?? null,
                            createdAt: t.createdAt ?? null,
                            modifiedBy: t.modifiedBy ?? null,
                            modifiedAt: t.modifiedAt ?? null,
                            version: t.version ?? 1
                        }));
                    }
                }

                try {
                    const emps = await api.fetchEmployees();
                    if (mounted) setEmployees(Array.isArray(emps) ? emps : []);
                } catch (e) {
                    console.warn('Не удалось загрузить сотрудников:', e);
                    setEmployees([]);
                }

                try {
                    const projs = await api.fetchProjects();
                    if (mounted) setProjects(Array.isArray(projs) ? projs : []);
                } catch (e) {
                    console.warn('Не удалось загрузить проекты:', e);
                    setProjects([]);
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

    const columnDefs = useMemo(() => [
        { headerName: 'Сотрудник', field: 'employeeFullName', sortable: true, filter: true, width: 100, flex: 1 },
        { headerName: 'Проект', field: 'projectCode', sortable: true, filter: true, width: 100 },
        {
            headerName: 'Дата', field: 'date', sortable: true, filter: true, width: 100,
            valueFormatter: (p: any) => p.value ? new Date(p.value).toLocaleDateString() : ''
        },
        { headerName: 'Часы', field: 'hours', sortable: true, filter: true, width: 100 },
        { headerName: 'Ожидаемая стоимость', field: 'expectedCost', sortable: true, filter: true, width: 140 },
        { headerName: 'Комментарий', field: 'comment', width: 140 },
        { headerName: 'Создал', field: 'createdBy', width: 100 },
        { headerName: 'Версия', field: 'version', width: 100}
    ], []);
    console.log(projects);
    const onSubmit = async (values: any, { setSubmitting, resetForm }: any) => {
        setError(null);
        try {

            const payload = {
                employeeId: values.employeeId,
                projectId: values.projectId,
                date: values.date,
                hours: Number(values.hours),
                comment: values.comment,
                createdBy: 'currentUser'
            };

            if ((store as any).addTimeEntry) {
                await (store as any).addTimeEntry(payload);
            } else {
                await api.createTimeEntry(payload);
                if ((store as any).fetchTimeEntries) await (store as any).fetchTimeEntries();
            }

            resetForm();
        } catch (e: any) {
            console.error('Create time entry error', e);
            setError(e?.message ?? 'Ошибка при создании записи');
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <Button minimal onClick={async () => { setLoading(true); try { await (store as any).fetchTimeEntries(); } finally { setLoading(false); } }}>Обновить</Button>
            </div>

            {loading && <div>Загрузка...</div>}
            {error && <div style={{ color: 'red' }}>{error}</div>}

            {!loading && !error && (
                <>
                    <div className="ag-theme-alpine" style={{ height: 360, width: '100%' }}>
                        <AgGridReact
                            rowData={rows}
                            columnDefs={columnDefs}
                            pagination
                            paginationPageSize={25}
                            defaultColDef={{ resizable: true }}
                        />
                    </div>

                    <div style={{ marginTop: 12 }}>
                        <h4>Добавить запись табеля</h4>

                        <Formik
                            initialValues={{ employeeId: '', projectId: '', date: new Date().toISOString().slice(0, 10), hours: 8, comment: '' }}
                            validationSchema={EntrySchema}
                            onSubmit={onSubmit}
                        >
                            {({ isSubmitting }) => (
                                <Form>
                                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                                        <div>
                                            <FormGroup label="Сотрудник" labelFor="employeeId">
                                                <Field name="employeeId" as="select" id="employeeId" className="bp3-input">
                                                    <option value="">-- выберите сотрудника --</option>
                                                    {employees.map((e: any) => (
                                                        <option key={e.id ?? e._id ?? e.id} value={e.id ?? e._id ?? e.id}>
                                                            {(e.fullName ?? '')}
                                                        </option>
                                                    ))}
                                                </Field>
                                                <div style={{ color: 'red' }}><ErrorMessage name="employeeId" /></div>
                                            </FormGroup>
                                        </div>

                                        <div>
                                            <FormGroup label="Проект" labelFor="projectId">
                                                <Field name="projectId" as="select" id="projectId" className="bp3-input">
                                                    <option value="">-- выберите проект --</option>
                                                    {projects.map((p: any) => (
                                                        <option key={p.id ?? p._id ?? p.id} value={p.id ?? p._id ?? p.id}>
                                                            {p.code ?? p.id}
                                                        </option>
                                                    ))}
                                                </Field>
                                                <div style={{ color: 'red' }}><ErrorMessage name="projectId" /></div>
                                            </FormGroup>
                                        </div>

                                        <div>
                                            <FormGroup label="Дата" labelFor="date">
                                                <Field name="date" as={InputGroup} id="date" type="date" />
                                                <div style={{ color: 'red' }}><ErrorMessage name="date" /></div>
                                            </FormGroup>
                                        </div>

                                        <div>
                                            <FormGroup label="Часы" labelFor="hours">
                                                <Field name="hours" as={InputGroup} id="hours" type="number" step="0.5" />
                                                <div style={{ color: 'red' }}><ErrorMessage name="hours" /></div>
                                            </FormGroup>
                                        </div>

                                        <div style={{ gridColumn: '1 / -1' }}>
                                            <FormGroup label="Комментарий" labelFor="comment">
                                                <Field name="comment" as={InputGroup} id="comment" />
                                            </FormGroup>
                                        </div>

                                        <div style={{ gridColumn: '1 / -1', display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                                            <Button type="submit" intent={Intent.PRIMARY} loading={isSubmitting}>Добавить</Button>
                                            <Button type="reset">Сброс</Button>
                                        </div>
                                    </div>
                                </Form>
                            )}
                        </Formik>
                    </div>
                </>
            )}
        </div>
    );
};

export default TimeEntryList;