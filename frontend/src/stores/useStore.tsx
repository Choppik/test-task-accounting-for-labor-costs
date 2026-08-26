import React from 'react';
import { RootStore } from './rootStore';

const StoreContext = React.createContext<RootStore | null>(null);

export const StoreProvider: React.FC<{ store: RootStore }> = ({ store, children }) => (
    <StoreContext.Provider value={store}>{children}</StoreContext.Provider>
);

export function useStore(): RootStore {
    const s = React.useContext(StoreContext);
    if (!s) throw new Error('StoreProvider is missing');
    return s;
}