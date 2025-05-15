import React, { useState, useEffect } from 'react';

// Funkcja do generowania dostêpnych miejsc w biurze
const getAvailableDesks = (date) => {
    const mockData = {
        '2025-05-10': 10, // Dostêpnych 10 miejsc na ten dzieñ
        '2025-05-11': 8,
        '2025-05-12': 12,
        '2025-05-13': 9,
        '2025-05-14': 7,
        '2025-05-15': 11,
        '2025-05-16': 5,
    };

    return mockData[date] || 0; // Zwracamy liczbê dostêpnych miejsc
};

function Availability() {
    const [availabilityData, setAvailabilityData] = useState(0);
    const [futureAvailability, setFutureAvailability] = useState([]);

    useEffect(() => {
        // Zbieramy dane dostêpnoœci dla dzisiejszego dnia
        const formattedDate = new Date().toISOString().split('T')[0]; // U¿ywamy formatu "YYYY-MM-DD"
        setAvailabilityData(getAvailableDesks(formattedDate));

        // Zbieramy dane dostêpnoœci na kolejne 7 dni
        const availabilityForNext7Days = [];
        for (let i = 1; i <= 7; i++) {
            const futureDate = new Date();
            futureDate.setDate(futureDate.getDate() + i);
            const futureFormattedDate = futureDate.toISOString().split('T')[0];
            availabilityForNext7Days.push({
                date: futureFormattedDate,
                available: getAvailableDesks(futureFormattedDate),
            });
        }

        setFutureAvailability(availabilityForNext7Days);
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
