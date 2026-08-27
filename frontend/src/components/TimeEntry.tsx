import React from 'react';
import { Dialog, Classes, Button, Intent, FormGroup, InputGroup } from '@blueprintjs/core';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import * as Yup from 'yup';
import * as api from '../utils/api';

type Props = {
    isOpen: boolean;
    onClose: () => void;
    onSaved: () => Promise<void>;
    initial?: any;
    employees: any[];
    projects: any[];
    store: any;
};

const isMultipleOfHalf = (n: number) => Math.abs(Math.round(n * 2) - n * 2) < 1e-8;

const TimeEntry: React.FC<Props> = ({ isOpen, onClose, onSaved, initial, employees, projects, store }) => {
    const isEdit = !!initial;

    const getProjectById = (id: string) => projects.find((p: any) => (p.id ?? p._id ?? p.id) === id);

    const Schema = Yup.object().shape({
        employeeId: Yup.string().required('Сотрудник обязателен'),
        projectId: Yup.string().required('Проект обязателен'),
        date: Yup.string().required('Дата обязательна'),
        hours: Yup.number().required('Часы обязательны')
            .min(0.5, 'Минимум 0.5')
            .max(24, 'Максимум 24')
            .test('half', 'Часы должны быть кратны 0.5', (v: any) => isMultipleOfHalf(Number(v)))
    }).test('project-date', 'Дата должна быть в диапазоне проекта', function (values: any) {
        const proj = getProjectById(values.projectId);
        if (!proj) return true;
        const date = new Date(values.date);
        const start = proj.startDate ? new Date(proj.startDate) : null;
        const end = proj.endDate ? new Date(proj.endDate) : null;
        if (start && date < start) return this.createError({ path: 'date', message: `Дата раньше начала проекта (${start.toLocaleDateString()})` });
        if (end && date > end) return this.createError({ path: 'date', message: `Дата позже окончания проекта (${end.toLocaleDateString()})` });
        return true;
    });

    const initialValues = {
        id: initial?.id ?? '',
        employeeId: initial?.employeeId ?? '',
        projectId: initial?.projectId ?? '',
        date: initial ? (initial.date?.slice(0, 10) ?? '') : new Date().toISOString().slice(0, 10),
        hours: initial?.hours ?? 8,
        comment: initial?.comment ?? ''
    };

    return (
        <Dialog isOpen={isOpen} onClose={onClose} title={isEdit ? 'Редактировать запись' : 'Создать запись'}>
            <div className={Classes.DIALOG_BODY} style={{ padding: 12 }}>
                <Formik
                    initialValues={initialValues}
                    validationSchema={Schema}
                    enableReinitialize
                    onSubmit={async (values, { setSubmitting }) => {
                        try {
                            const payload = {
                                employeeId: values.employeeId,
                                projectId: values.projectId,
                                date: values.date,
                                hours: Number(values.hours),
                                comment: values.comment
                            };

                            if (isEdit) {
                                if (store.updateTimeEntry) {
                                    await store.updateTimeEntry(values.id, { ...payload, version: initial.version, modifiedBy: 'currentUserM', modifiedAt: new Date().toISOString().slice(0, 10) });
                                } else {
                                    await api.updateTimeEntry(values.id, { ...payload, modifiedBy: 'currentUserM', version: initial.version, modifiedAt: new Date().toISOString().slice(0, 10) });
                                    if (store.fetchTimeEntries) await store.fetchTimeEntries();
                                }
                            } else {
                                if (store.addTimeEntry) {
                                    await store.addTimeEntry({ ...payload, createdBy: 'currentUser', createdAt: new Date().toISOString().slice(0, 10)});
                                } else {
                                    await api.createTimeEntry({ ...payload, createdBy: 'currentUser', createdAt: new Date().toISOString().slice(0, 10)});
                                    if (store.fetchTimeEntries) await store.fetchTimeEntries();
                                }
                            }

                            await onSaved();
                            onClose();
                        } catch (err: any) {
                            alert('Ошибка: ' + (err?.message ?? 'Не удалось сохранить'));
                        } finally {
                            setSubmitting(false);
                        }
                    }}
                >
                    {({ isSubmitting }) => (
                        <Form>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                                <div>
                                    <FormGroup label="Сотрудник" labelFor="employeeId">
                                        <Field as="select" name="employeeId" className="bp3-input">
                                            <option value="">-- выберите --</option>
                                            {employees.map((e: any) => (
                                                <option key={e.id ?? e._id ?? e.id} value={e.id ?? e._id ?? e.id}>
                                                    {e.fullName}
                                                </option>
                                            ))}
                                        </Field>
                                        <div style={{ color: 'red' }}><ErrorMessage name="employeeId" /></div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Проект" labelFor="projectId">
                                        <Field as="select" name="projectId" className="bp3-input">
                                            <option value="">-- выберите --</option>
                                            {projects.map((p: any) => (
                                                <option key={p.id ?? p._id ?? p.id} value={p.id ?? p._id ?? p.id}>
                                                    {p.name ?? p.code}
                                                </option>
                                            ))}
                                        </Field>
                                        <div style={{ color: 'red' }}><ErrorMessage name="projectId" /></div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Дата" labelFor="date">
                                        <Field name="date" as={InputGroup} type="date" />
                                        <div style={{ color: 'red' }}><ErrorMessage name="date" /></div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Часы" labelFor="hours">
                                        <Field name="hours" as={InputGroup} type="number" step="0.5" />
                                        <div style={{ color: 'red' }}><ErrorMessage name="hours" /></div>
                                    </FormGroup>
                                </div>

                                <div style={{ gridColumn: '1 / -1' }}>
                                    <FormGroup label="Комментарий" labelFor="comment">
                                        <Field name="comment" as={InputGroup} />
                                    </FormGroup>
                                </div>

                                <div style={{ gridColumn: '1 / -1', display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                                    <Button type="submit" intent={Intent.PRIMARY} loading={isSubmitting}>{isEdit ? 'Подтвердить' : 'Добавить'}</Button>
                                    <Button onClick={onClose}>Отмена</Button>
                                </div>
                            </div>
                        </Form>
                    )}
                </Formik>
            </div>
        </Dialog>
    );
};

export default TimeEntry;