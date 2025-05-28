import React, { useState } from 'react';
import axios from 'axios';

export default function AdminPanel() {
    const [seedResult, setSeedResult] = useState(null);
    const [assignmentResult, setAssignmentResult] = useState(null);
    const [selectedDate, setSelectedDate] = useState(() => new Date().toISOString().split('T')[0]);
    const [reservationCount, setReservationCount] = useState(200);
    const [loading, setLoading] = useState(false);

    const seedDatabase = async () => {
        setLoading(true);
        try {
            const res = await axios.post(`/api/admin/seed?date=${selectedDate}&count=${reservationCount}`);
            setSeedResult(res.data);
            setAssignmentResult(null); // wyczyść poprzednie przypisania
        } catch (err) {
            console.error(err);
            alert("Seeding failed.");
        } finally {
            setLoading(false);
        }
    };

    const runAlgorithm = async () => {
        setLoading(true);
        try {
            const res = await axios.post(`/api/admin/assign?date=${selectedDate}`);
            setAssignmentResult(res.data);
        } catch (err) {
            console.error(err);
            alert("Assignment failed.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="p-4">
            <h2 className="text-xl font-bold mb-4">Admin Panel</h2>

            <div className="flex items-center space-x-6 mb-4">
                <div>
                    <label htmlFor="date" className="block text-sm font-medium">Reservation Date:</label>
                    <input
                        type="date"
                        id="date"
                        value={selectedDate}
                        onChange={e => setSelectedDate(e.target.value)}
                        className="border p-2 rounded w-40"
                    />
                </div>

                <div>
                    <label htmlFor="count" className="block text-sm font-medium">Reservation Count:</label>
                    <input
                        type="number"
                        id="count"
                        min={1}
                        max={1000}
                        value={reservationCount}
                        onChange={e => setReservationCount(e.target.value)}
                        className="border p-2 rounded w-24"
                    />
                </div>
            </div>

            <button onClick={seedDatabase} className="btn btn-primary mr-4" disabled={loading}>
                {loading ? 'Seeding...' : 'Seed Database'}
            </button>
            <button onClick={runAlgorithm} className="btn btn-secondary" disabled={loading}>
                {loading ? 'Assigning...' : 'Run Assignment'}
            </button>

            {loading && (
                <div className="flex justify-center mt-4">
                    <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-gray-900"></div>
                </div>
            )}

            {assignmentResult && !loading && (
                <div className="mt-6">
                    <h3 className="text-lg font-semibold mb-4">Assignments:</h3>
                    {assignmentResult.assigned && (
                        <div className="space-y-6">
                            {Object.entries(
                                assignmentResult.assigned.reduce((grouped, r) => {
                                    if (!grouped[r.team]) grouped[r.team] = [];
                                    grouped[r.team].push(r);
                                    return grouped;
                                }, {})
                            ).map(([teamName, assignments]) => (
                                <div key={teamName}>
                                    <h4 className="text-lg font-bold border-b pb-1 mb-2">{teamName}</h4>
                                    <div className="space-y-1">
                                        {assignments.map((r, idx) => (
                                            <div key={idx} className="border-b py-2">
                                                <strong>{r.name}</strong> ({r.email}) → <em>{r.deskName}</em> in zone <em>{r.zone}</em>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {assignmentResult.failed?.length > 0 && (
                        <div className="mt-6 text-red-600">
                            <h4 className="font-semibold mb-2">Failed Assignments:</h4>
                            <ul className="list-disc list-inside">
                                {assignmentResult.failed.map((f, i) => <li key={i}>{f}</li>)}
                            </ul>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
