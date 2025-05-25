import React, { useState, useEffect } from 'react';

// Funkcja do pobierania dostępnych miejsc w biurze
const getAvailableDesks = async (date) => {
    try {
        const res = await fetch(`/api/reservations/availability?date=${date}`);
        const data = await res.json();
        return {
            all: data.all,
            focus: data.focus
        };
    } catch (err) {
        console.error("Error fetching availability:", err);
        return {
            all: { free: 0, total: 0 },
            focus: { free: 0, total: 0 }
        };
    }
};

function Availability() {
    const [availabilityData, setAvailabilityData] = useState(null);
    const [futureAvailability, setFutureAvailability] = useState([]);

    useEffect(() => {
        const loadAvailability = async () => {
            const today = new Date().toISOString().split('T')[0];
            const todayAvailable = await getAvailableDesks(today);
            setAvailabilityData(todayAvailable);

            const future = await Promise.all(
                Array.from({ length: 7 }).map((_, i) => {
                    const d = new Date();
                    d.setDate(d.getDate() + i + 1);
                    const dateStr = d.toISOString().split('T')[0];
                    return getAvailableDesks(dateStr).then(available => ({
                        date: dateStr,
                        ...available
                    }));
                })
            );

            setFutureAvailability(future);
        };

        loadAvailability();
    }, []);


    return (
        <div className="max-w-2xl mx-auto p-8 bg-white dark:bg-gray-800 rounded-2xl shadow-md">
            <h2 className="text-2xl font-bold mb-6">Available Desks</h2>

            <h3 className="text-xl font-semibold mb-4">Available Desks Today</h3>
            {availabilityData && (
                <div className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex flex-col space-y-1">
                    <p>💼 All: <strong>{availabilityData.all.free}</strong> / {availabilityData.all.total}</p>
                    <p>🧠 Focus: <strong>{availabilityData.focus.free}</strong> / {availabilityData.focus.total}</p>
                </div>
            )}

            <h3 className="text-xl font-semibold mt-6 mb-4">Available Desks for the Next 7 Days</h3>
            <div className="space-y-4">
                {futureAvailability.map((item, index) => (
                    <div key={index} className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex flex-col space-y-1">
                        <h3 className="font-semibold">{item.date}</h3>
                        <p>💼 All: <strong>{item.all.free}</strong> / {item.all.total}</p>
                        <p>🧠 Focus: <strong>{item.focus.free}</strong> / {item.focus.total}</p>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default Availability;
