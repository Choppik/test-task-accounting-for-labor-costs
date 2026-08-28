import React, { useState } from 'react';
import {
    Dialog,
    Classes,
    Button,
    Intent,
    FormGroup,
    InputGroup,
    Callout,
} from '@blueprintjs/core';
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

const isMultipleOfHalf = (n: number) =>
    Math.abs(Math.round(n * 2) - n * 2) < 1e-8;

const TimeEntry: React.FC<Props> = ({
        isOpen,
        onClose,
        onSaved,
        initial,
        employees,
        projects,
        store,
    }) => {
    const isEdit = !!initial;
    const [serverError, setServerError] = useState<string | null>(null);

    const Schema = Yup.object().shape({
        employeeId: Yup.string().required('Сотрудник обязателен'),
        projectId: Yup.string().required('Проект обязателен'),
        date: Yup.string().required('Дата обязательна'),
        hours: Yup.number()
            .required('Часы обязательны')
            .min(0.5, 'Минимум 0.5')
            .max(24, 'Максимум 24')
            .test('half', 'Часы должны быть кратны 0.5', (v: any) =>
                isMultipleOfHalf(Number(v))
            ),
    });

    const initialValues = {
        id: initial?.id ?? '',
        employeeId: initial?.employeeId ?? '',
        projectId: initial?.projectId ?? '',
        date: initial
            ? initial.date?.slice(0, 10) ?? ''
            : new Date().toISOString().slice(0, 10),
        hours: initial?.hours ?? 8,
        comment: initial?.comment ?? '',
    };

    return (
        <Dialog
            isOpen={isOpen}
            onClose={onClose}
            title={isEdit ? 'Редактировать запись' : 'Создать запись'}
        >
            <div className={Classes.DIALOG_BODY} style={{ padding: 12 }}>
                <Formik
                    initialValues={initialValues}
                    validationSchema={Schema}
                    enableReinitialize
                    onSubmit={async (values, { setSubmitting, setErrors }) => {
                        // Сбрасываем предыдущую ошибку перед новым запросом
                        setServerError(null);

                        try {
                            const payload = {
                                employeeId: values.employeeId,
                                projectId: values.projectId,
                                date: values.date,
                                hours: Number(values.hours),
                                comment: values.comment,
                            };

                            if (isEdit) {
                                if (store.updateTimeEntry) {
                                    await store.updateTimeEntry(values.id, {
                                        ...payload,
                                        version: initial.version,
                                        modifiedBy: 'currentUserM',
                                        modifiedAt: new Date().toISOString().slice(0, 10),
                                    });
                                } else {
                                    await api.updateTimeEntry(values.id, {
                                        ...payload,
                                        modifiedBy: 'currentUserM',
                                        version: initial.version,
                                        modifiedAt: new Date().toISOString().slice(0, 10),
                                    });
                                    if (store.fetchTimeEntries) await store.fetchTimeEntries();
                                }
                            } else {
                                if (store.addTimeEntry) {
                                    await store.addTimeEntry({
                                        ...payload,
                                        createdBy: 'currentUser',
                                        createdAt: new Date().toISOString().slice(0, 10),
                                    });
                                } else {
                                    await api.createTimeEntry({
                                        ...payload,
                                        createdBy: 'currentUser',
                                        createdAt: new Date().toISOString().slice(0, 10),
                                    });
                                    if (store.fetchTimeEntries) await store.fetchTimeEntries();
                                }
                            }

                            await onSaved();
                            onClose();
                        } catch (err: any) {
                            let message = '';

                            if (err?.response?.data?.message) {
                                message = err.response.data.message;
                            } else if (err?.message) {
                                message = err.message;
                            } else {
                                message = 'Не удалось сохранить запись. Проверьте соединение или попробуйте позже.';
                            }

                            // ВАЖНО: Все ошибки (и ставка, и БД, и сеть) идут сюда
                            setServerError(message);
                        } finally {
                            setSubmitting(false);
                        }
                    }}
                >
                    {({ isSubmitting }) => (
                        <Form>
                            {serverError && (
                                <div
                                    style={{
                                        padding: 12,
                                        backgroundColor: '#ffebee', 
                                        color: '#c62828',        
                                        borderRadius: 4,
                                        marginBottom: 16,
                                        fontSize: 14,
                                    }}
                                >
                                    {serverError}
                                </div>
                            )}

                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                                <div>
                                    <FormGroup label="Сотрудник" labelFor="employeeId">
                                        <Field as="select" name="employeeId" className="bp3-input">
                                            <option value="">-- выберите --</option>
                                            {employees.map((e: any) => (
                                                <option
                                                    key={e.id ?? e._id ?? e.id}
                                                    value={e.id ?? e._id ?? e.id}
                                                >
                                                    {e.fullName}
                                                </option>
                                            ))}
                                        </Field>
                                        <div style={{ color: 'red', fontSize: '12px', marginTop: 4 }}>
                                            <ErrorMessage name="employeeId" />
                                        </div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Проект" labelFor="projectId">
                                        <Field as="select" name="projectId" className="bp3-input">
                                            <option value="">-- выберите --</option>
                                            {projects.map((p: any) => (
                                                <option
                                                    key={p.id ?? p._id ?? p.id}
                                                    value={p.id ?? p._id ?? p.id}
                                                >
                                                    {p.name ?? p.code}
                                                </option>
                                            ))}
                                        </Field>
                                        <div style={{ color: 'red', fontSize: '12px', marginTop: 4 }}>
                                            <ErrorMessage name="projectId" />
                                        </div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Дата" labelFor="date">
                                        <Field name="date" as={InputGroup} type="date" />
                                        <div style={{ color: 'red', fontSize: '12px', marginTop: 4 }}>
                                            <ErrorMessage name="date" />
                                        </div>
                                    </FormGroup>
                                </div>

                                <div>
                                    <FormGroup label="Часы" labelFor="hours">
                                        <Field
                                            name="hours"
                                            as={InputGroup}
                                            type="number"
                                            step="0.5"
                                        />
                                        <div style={{ color: 'red', fontSize: '12px', marginTop: 4 }}>
                                            <ErrorMessage name="hours" />
                                        </div>
                                    </FormGroup>
                                </div>

                                <div style={{ gridColumn: '1 / -1' }}>
                                    <FormGroup label="Комментарий" labelFor="comment">
                                        <Field name="comment" as={InputGroup} />
                                    </FormGroup>
                                </div>

                                <div
                                    style={{
                                        gridColumn: '1 / -1',
                                        display: 'flex',
                                        gap: 8,
                                        justifyContent: 'flex-end',
                                    }}
                                >
                                    <Button
                                        type="submit"
                                        intent={Intent.PRIMARY}
                                        loading={isSubmitting}
                                    >
                                        {isEdit ? 'Подтвердить' : 'Добавить'}
                                    </Button>
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