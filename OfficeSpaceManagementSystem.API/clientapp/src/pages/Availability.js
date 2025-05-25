import React, { useState, useEffect } from 'react';

// Funkcja do generowania dostêpnych miejsc w biurze
const getAvailableDesks = async (date) => {
    try {
        const res = await fetch(`/api/availability?date=${date}`);
        const data = await res.json();
        return data.available;
    } catch (err) {
        console.error("Error fetching availability:", err);
        return 0;
    }
};


function Availability() {
    const [availabilityData, setAvailabilityData] = useState(0);
    const [futureAvailability, setFutureAvailability] = useState([]);

    useEffect(() => {
        const loadAvailability = async () => {
            const today = new Date().toISOString().split('T')[0];
            const todayAvailable = await getAvailableDesks(today);
            setAvailabilityData(todayAvailable);

            const future = [];
            for (let i = 1; i <= 7; i++) {
                const d = new Date();
                d.setDate(d.getDate() + i);
                const dateStr = d.toISOString().split('T')[0];
                const available = await getAvailableDesks(dateStr);
                future.push({ date: dateStr, available });
            }

            setFutureAvailability(future);
        };

        loadAvailability();
    }, []);

    return (
        <div className="max-w-2xl mx-auto p-8 bg-white dark:bg-gray-800 rounded-2xl shadow-md">
            <h2 className="text-2xl font-bold mb-6">Available Desks</h2>

            <h3 className="text-xl font-semibold mb-4">Available Desks Today</h3>
            <div className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex justify-between">
                <div>
                    <h3 className="font-semibold">Today</h3>
                </div>
                <div className="text-right">
                    <p className="font-semibold">{availabilityData} available</p>
                </div>
            </div>

            <h3 className="text-xl font-semibold mt-6 mb-4">Available Desks for the Next 7 Days</h3>
            <div className="space-y-4">
                {futureAvailability.map((item, index) => (
                    <div key={index} className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex justify-between">
                        <div>
                            <h3 className="font-semibold">{item.date}</h3>
                        </div>
                        <div className="text-right">
                            <p className="font-semibold">{item.available} available</p>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default Availability;
