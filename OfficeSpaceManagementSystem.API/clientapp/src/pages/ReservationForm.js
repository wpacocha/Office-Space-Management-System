import React, { useState } from "react";
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";

export default function ReservationForm() {
    const [date, setDate] = useState("");
    const [deskTypePref, setDeskTypePref] = useState(0);
    const [teamName, setTeamName] = useState("");
    const [suggestions, setSuggestions] = useState([]);
    const [message, setMessage] = useState("");
    const [focusMode, setFocusMode] = useState(false); // Stan checkboxa dla trybu skupienia

    const handleSubmit = async (e) => {
        e.preventDefault();

        const res = await fetch("/reservations", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                date,
                deskTypePref,
                teamName: focusMode ? "" : teamName, // Jeśli Focus Mode jest zaznaczone, nie musimy podawać nazwy zespołu
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

    const handleTeamNameChange = (e) => {
        const value = e.target.value;
        setTeamName(value);
        if (value.length >= 2) {
            setSuggestions([]); // symulujemy puste podpowiedzi
        } else {
            setSuggestions([]);
        }
    };

    const isFormValid = date && deskTypePref !== "" && (focusMode || teamName.trim() !== "");

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
                        minDate={new Date()}
                        maxDate={new Date(new Date().setDate(new Date().getDate() + 7))}
                        className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700"
                        dayClassName={date =>
                            date < new Date() || date > new Date(new Date().setDate(new Date().getDate() + 7))
                                ? "text-gray-400" // szary tekst dla niedostępnych dni
                                : undefined
                        }
                    />
                </div>

                <div>
                    <label className="block mb-2 font-semibold">Desk Type</label>
                    <select
                        value={deskTypePref}
                        onChange={(e) => setDeskTypePref(Number(e.target.value))}
                        className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-primary transition"
                    >
                        <option value={0}>Standard</option>
                        <option value={1}>Dual Monitor</option>
                        <option value={2}>Supercharged</option>
                    </select>
                </div>

                {/* Checkbox Focus Mode */}
                <div className="flex items-center space-x-2">
                    <input
                        type="checkbox"
                        id="focusMode"
                        checked={focusMode}
                        onChange={() => setFocusMode(!focusMode)}
                        className="h-5 w-5"
                    />
                    <label htmlFor="focusMode" className="font-semibold">
                        I want a quiet area (Focus Mode)
                    </label>
                </div>

                {/* Pole "Team Name" jest widoczne tylko jeśli Focus Mode nie jest zaznaczone */}
                {!focusMode && (
                    <div>
                        <label className="block mb-2 font-semibold">Team Name</label>
                        <input
                            type="text"
                            value={teamName}
                            onChange={handleTeamNameChange}
                            list="team-suggestions"
                            placeholder="Enter team name"
                            required
                            className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-primary transition"
                        />
                        <datalist id="team-suggestions">
                            {suggestions.map((team, idx) => (
                                <option key={idx} value={team} />
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
