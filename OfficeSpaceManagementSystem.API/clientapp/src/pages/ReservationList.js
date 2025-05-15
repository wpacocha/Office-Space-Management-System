import { ClipLoader } from 'react-spinners';
import React, { useState, useEffect } from 'react';

function ReservationList() {
    const [loading, setLoading] = useState(true);
    const [reservations, setReservations] = useState([]);

    useEffect(() => {
        setTimeout(() => {
            setReservations([
                { date: '2025-05-10', zonePreference: 1, deskTypePref: 'Standard' },
                { date: '2025-05-12', zonePreference: 2, deskTypePref: 'Dual Monitor' },
            ]);
            setLoading(false);
        }, 2000);
    }, []);

    return (
        <div className="max-w-2xl mx-auto p-8 bg-white dark:bg-gray-800 rounded-2xl shadow-md">
            <h2 className="text-2xl font-bold mb-6">My Reservations</h2>
            {loading ? (
                <div className="flex justify-center items-center h-96">
                    <ClipLoader color="#6366f1" loading={loading} size={50} />
                </div>
            ) : (
                <div className="space-y-4">
                    {reservations.map((res, index) => (
                        <div key={index} className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex justify-between">
                            <div>
                                <h3 className="font-semibold">{res.date}</h3>
                                <p>Zone: {res.zonePreference}</p>
                            </div>
                            <div className="text-right">
                                <p>Desk Type: {res.deskTypePref}</p>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default ReservationList;
