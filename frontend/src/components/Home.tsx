import React, { useState } from 'react';
import { Button, Classes, Dialog, H3 } from '@blueprintjs/core';
import TimeEntryList from './TimeEntryList';
import ReportProject from './ReportProject';

const Home: React.FC = () => {
    const [openTimeEntry, setOpenTimeEntry] = useState(false);
    const [openReports, setOpenReports] = useState(false);

    return (
        <div style={{ minHeight: '70vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <div style={{ width: 600, textAlign: 'center' }}>
                <H3 style={{ marginBottom: 16 }}>Управление</H3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12, alignItems: 'center'}}>
                    <Button intent="success" large onClick={() => setOpenTimeEntry(true)}>
                        Записи табеля
                    </Button>

                    <Button intent="success" large onClick={() => setOpenReports(true)}>
                        Отчеты
                    </Button>

                    <Button intent="success" large onClick={() => setOpenTimeEntry(true)}>
                        Справочники
                    </Button>
                </div>
            </div>

            <Dialog
                icon="list"
                isOpen={openTimeEntry}
                title="Записи табеля"
                onClose={() => setOpenTimeEntry(false)}
                canOutsideClickClose
                canEscapeKeyClose
                style={{ width: '80%', maxWidth: 1000 }}
            >
                <div className={Classes.DIALOG_BODY} style={{ padding: 12 }}>
                    <TimeEntryList onClose={() => setOpenTimeEntry(false)} />
                </div>
            </Dialog>

            <Dialog
                icon="list"
                isOpen={openReports}
                title="Отчеты"
                onClose={() => setOpenReports(false)}
                canOutsideClickClose
                canEscapeKeyClose
                style={{ width: '80%', maxWidth: 1000 }}
            >
                <div className={Classes.DIALOG_BODY} style={{ padding: 12 }}>
                    <ReportProject onClose={() => setOpenReports(false)} />
                </div>
            </Dialog>
        </div>
    );
};

export default Home;