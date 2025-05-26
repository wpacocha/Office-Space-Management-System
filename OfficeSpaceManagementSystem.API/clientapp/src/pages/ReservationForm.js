import React, { useState, useEffect } from "react";
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";
import { registerLocale } from "react-datepicker";
import pl from "date-fns/locale/pl";
registerLocale("pl", pl);

export default function ReservationForm() {
    const [date, setDate] = useState("");
    const [deskTypePref, setDeskTypePref] = useState(0);
    const [teamName, setTeamName] = useState("");
    const [suggestions, setSuggestions] = useState([]);
    const [message, setMessage] = useState("");
    const [focusMode, setFocusMode] = useState(false);
    const [focusAvailable, setFocusAvailable] = useState(null);

    // Ładowanie podpowiedzi zespołów
    useEffect(() => {
        if (teamName.length < 2) {
            setSuggestions([]);
            return;
        }

        const timeoutId = setTimeout(() => {
            fetch(`/api/teams?prefix=${teamName}`)
                .then(res => res.json())
                .then(data => setSuggestions(data))
                .catch(() => setSuggestions([]));
        }, 300);

        return () => clearTimeout(timeoutId);
    }, [teamName]);

    // Sprawdzanie dostępności Focus Mode
    useEffect(() => {
        if (!date) {
            setFocusAvailable(null);
            return;
        }

        const fetchFocus = async () => {
            try {
                const res = await fetch(`/api/reservations/availability?date=${date}`);
                const data = await res.json();
                setFocusAvailable(data.focus.free);
                if (data.focus.free === 0) setFocusMode(false); // automatycznie odznacz, jeśli niedostępne
            } catch {
                setFocusAvailable(null);
            }
        };

        fetchFocus();
    }, [date]);

    const handleSubmit = async (e) => {
        e.preventDefault();

        const res = await fetch("/api/reservations", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                date,
                deskTypePref,
                isFocusMode: focusMode,
                teamName: focusMode ? "" : teamName,
                userId: 1
            }),
        });

        if (res.ok) {
            setMessage("✅ Reservation sent!");
        } else {
            const err = await res.text();
            setMessage(`❌ Error: ${err}`);
        }
    };

    const isFormValid = date && deskTypePref !== "" && (focusMode || teamName.trim() !== "");
    const now = new Date();
    const cutoffPassed = now.getHours() >= 15;

    const minDate = new Date();
    minDate.setDate(minDate.getDate() + (cutoffPassed ? 2 : 1));

    return (
        <div className="max-w-2xl mx-auto p-8 bg-white dark:bg-gray-800 rounded-2xl shadow-md">
            <h2 className="text-2xl font-bold mb-6">Reserve a Desk</h2>
            <form onSubmit={handleSubmit} className="space-y-6">
                <div>
                    <label className="block mb-2 font-semibold">Date</label>
                    <DatePicker
                        selected={date ? new Date(date) : null}
                        onChange={(date) => setDate(date.toISOString().split('T')[0])}
                        dateFormat="yyyy-MM-dd"
                        placeholderText="Select a date"
                        minDate={minDate}
                        maxDate={new Date(new Date().setDate(new Date().getDate() + 7))}
                        locale="pl"               
                        calendarStartDay={1}    
                        className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700"
                    />

                </div>

                <div>
                    <label className="block mb-2 font-semibold">Desk Type</label>
                    <select
                        value={deskTypePref}
                        onChange={(e) => setDeskTypePref(Number(e.target.value))}
                        className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700"
                    >
                        <option value={0}>Wide Monitor</option>
                        <option value={1}>Dual Monitor</option>
                    </select>
                </div>

                <div className="flex items-center space-x-2">
                    <input
                        type="checkbox"
                        id="focusMode"
                        checked={focusMode}
                        disabled={focusAvailable === 0}
                        onChange={() => setFocusMode(!focusMode)}
                        className="h-5 w-5"
                    />
                    <label htmlFor="focusMode" className="font-semibold">
                        I want a quiet area (Focus Mode)
                    </label>
                </div>

                {focusAvailable === 0 && (
                    <p className="text-red-600 text-sm">⚠️ No Focus desks available for selected date</p>
                )}

                {!focusMode && (
                    <div>
                        <label className="block mb-2 font-semibold">Team Name</label>
                        <input
                            type="text"
                            value={teamName}
                            onChange={(e) => setTeamName(e.target.value)}
                            list="team-suggestions"
                            placeholder="Enter team name"
                            required
                            className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700"
                        />
                        <datalist id="team-suggestions">
                            {suggestions.map((team, idx) => (
                                <option key={idx} value={team.name} />
                            ))}
                        </datalist>
                    </div>
                )}

                <button
                    type="submit"
                    disabled={!isFormValid}
                    className={`w-full p-3 rounded-lg font-semibold transition-colors duration-300 ${isFormValid
                        ? "bg-primary hover:bg-purple-700 text-white cursor-pointer"
                        : "bg-gray-400 text-gray-200 cursor-not-allowed"
                        }`}
                >
                    Reserve
                </button>
            </form>

            {message && <p className="mt-4 text-center">{message}</p>}
        </div>
    );
}
