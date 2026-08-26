import { types } from 'mobx-state-tree';

export const TimeEntryModel = types.model('TimeEntry', {
    id: types.identifier,
    employeeId: types.string,
    projectId: types.string,
    date: types.string,
    hours: types.number,
    expectedCost: types.maybe(types.number),
    comment: types.maybeNull(types.string),
    createdBy: types.maybeNull(types.string),
    createdAt: types.maybeNull(types.string),
    modifiedBy: types.maybeNull(types.string),
    modifiedAt: types.maybeNull(types.string),
    version: types.optional(types.number, 1)
});

export type TimeEntryModelType = typeof TimeEntryModel.Type;