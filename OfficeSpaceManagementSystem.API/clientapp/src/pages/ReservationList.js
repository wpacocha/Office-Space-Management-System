import { useState, useEffect } from 'react';

function ReservationList() {
    const [reservations, setReservations] = useState([]);
    const [error, setError] = useState(null);

    useEffect(() => {
        // Tymczasowe mockowane dane
        const mockData = [
            { date: '2025-05-10', zonePreference: 1, deskTypePref: 'Standard' },
            { date: '2025-05-12', zonePreference: 2, deskTypePref: 'DualMonitor' }
        ];

        // Udawaj fetch i ustaw dane
        setTimeout(() => {
            setReservations(mockData);
            setError(null);
        }, 500);
    }, []);

    return (
        <div>
            <h2>Moje rezerwacje</h2>
            {error && <p style={{ color: 'red' }}>B³¹d pobierania danych</p>}
            <table border="1">
                <thead>
                    <tr>
                        <th>Data</th>
                        <th>Strefa</th>
                        <th>Typ biurka</th>
                    </tr>
                </thead>
                <tbody>
                    {reservations.map((res, index) => (
                        <tr key={index}>
                            <td>{res.date}</td>
                            <td>Strefa {res.zonePreference}</td>
                            <td>{res.deskTypePref}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

export default ReservationList;
