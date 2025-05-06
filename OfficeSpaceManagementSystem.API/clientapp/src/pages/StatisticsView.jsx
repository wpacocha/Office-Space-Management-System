import React from 'react';

const stats = {
    totalReservations: 17,
    dualMonitor: 5,
    supercharged: 3,
    standard: 9,
};

function StatisticsView() {
    return (
        <div>
            <h2>Statystyki</h2>
            <p>📅 Łączna liczba rezerwacji: {stats.totalReservations}</p>
            <p>🖥️ Dual Monitor: {stats.dualMonitor}</p>
            <p>⚡ Supercharged: {stats.supercharged}</p>
            <p>🪑 Standard: {stats.standard}</p>
        </div>
    );
}

export default StatisticsView;
