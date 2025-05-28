import { ClipLoader } from 'react-spinners';
import React, { useState, useEffect } from 'react';

function ReservationList() {
    const [loading, setLoading] = useState(true);
    const [reservations, setReservations] = useState([]);

    useEffect(() => {
        fetch("/api/reservations?userId=1")
            .then(res => res.json())
            .then(data => {
                setReservations(data);
                setLoading(false);
            })
            .catch(err => {
                console.error("Error fetching reservations:", err);
                setLoading(false);
            });
    }, []);

    const deskTypeName = (type) => {
        if (type === 0) return "Wide Monitor";
        if (type === 1) return "Dual Monitor";
        return "Unknown";
    };

    const handleDelete = async (id) => {
        if (!window.confirm("Are you sure you want to cancel this reservation?")) return;

        const res = await fetch(`/api/reservations/${id}`, { method: "DELETE" });
        if (res.ok) {
            setReservations(reservations.filter(r => r.id !== id));
        } else {
            alert("Failed to delete reservation.");
        }
    };

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
                        <div key={index} className="p-4 bg-white dark:bg-gray-700 rounded-2xl shadow flex justify-between items-center">
                            <div>
                                <h3 className="font-semibold">{res.date}</h3>
                                <p>Desk Type: {res.assignedDeskType !== null ? deskTypeName(res.assignedDeskType) : "Not assigned"}</p>
                                <p>Focus Mode: {res.isFocusMode ? "Yes" : "No"}</p>
                                {res.assignedDeskName && <p>Desk: {res.assignedDeskName}</p>}
                            </div>
                            <button
                                onClick={() => handleDelete(res.id)}
                                className="px-4 py-2 bg-red-600 text-white rounded-xl hover:bg-red-700 transition"
                            >
                                Cancel
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default ReservationList;
