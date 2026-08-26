import React, { useState } from 'react';
import { Button, Classes, Dialog, H3 } from '@blueprintjs/core';
import TimeEntryList from './TimeEntryList';

const Home: React.FC = () => {
    const [open, setOpen] = useState(false);

    return (
        <div style={{ minHeight: '70vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <div style={{ width: 600, textAlign: 'center' }}>
                <H3 style={{ marginBottom: 16 }}>Управление</H3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12, alignItems: 'center' }}>
                    <Button intent="primary" large onClick={() => { /* пока пусто */ }}>
                        Функция 1 (пока пусто)
                    </Button>

                    <Button intent="success" large onClick={() => setOpen(true)}>
                        Просмотреть записи табеля
                    </Button>
                </div>
            </div>

            <Dialog
                icon="list"
                isOpen={open}
                title="Записи табеля"
                onClose={() => setOpen(false)}
                canOutsideClickClose
                canEscapeKeyClose
                style={{ width: '80%', maxWidth: 1000 }}
            >
                <div className={Classes.DIALOG_BODY} style={{ padding: 12 }}>
                    <TimeEntryList onClose={() => setOpen(false)} />
                </div>
            </Dialog>
        </div>
    );
};

export default Home;