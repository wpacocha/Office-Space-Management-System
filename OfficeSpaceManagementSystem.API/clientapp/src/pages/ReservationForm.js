import { useState } from "react";

export default function ReservationForm() {
    const [date, setDate] = useState("");
    const [deskTypePref, setDeskTypePref] = useState(0);
    const [zonePreference, setZonePreference] = useState(0);
    const [teamName, setTeamName] = useState("");
    const [suggestions, setSuggestions] = useState([]);
    const [message, setMessage] = useState("");

    const handleSubmit = async (e) => {
        e.preventDefault();

        const res = await fetch("/reservations", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                date,
                deskTypePref,
                zonePreference,
                teamName,
                userId: 1 // Na razie sztywno
            }),
        });

        if (res.ok) {
            setMessage("✅ Rezerwacja wysłana!");
        } else {
            const err = await res.text();
            setMessage(`❌ Błąd: ${err}`);
        }
    };

    const handleTeamNameChange = (e) => {
        const value = e.target.value;
        setTeamName(value);

        // Tu później będzie fetch z backendu
        if (value.length >= 2) {
            setSuggestions([]); // symulujemy puste podpowiedzi
        } else {
            setSuggestions([]);
        }
    };

    return (
        <div style={{ padding: "2rem" }}>
            <h2>Zarezerwuj biurko</h2>
            <form onSubmit={handleSubmit}>
                <div>
                    <label>Data: </label>
                    <input
                        type="date"
                        value={date}
                        onChange={(e) => setDate(e.target.value)}
                        required
                    />
                </div>
                <div>
                    <label>Typ biurka: </label>
                    <select
                        value={deskTypePref}
                        onChange={(e) => setDeskTypePref(Number(e.target.value))}
                    >
                        <option value={0}>Standard</option>
                        <option value={1}>Dual Monitor</option>
                        <option value={2}>Supercharged</option>
                    </select>
                </div>
                <div>
                    <label>Strefa preferowana: </label>
                    <select
                        value={zonePreference}
                        onChange={(e) => setZonePreference(Number(e.target.value))}
                    >
                        <option value={0}>Strefa 1</option>
                        <option value={1}>Strefa 2</option>
                        <option value={2}>Strefa 3</option>
                        <option value={3}>Strefa 4</option>
                    </select>
                </div>
                <div>
                    <label>Nazwa zespołu: </label>
                    <input
                        type="text"
                        value={teamName}
                        onChange={handleTeamNameChange}
                        list="team-suggestions"
                        placeholder="Wpisz nazwę zespołu"
                        required
                    />
                    <datalist id="team-suggestions">
                        {suggestions.map((team, idx) => (
                            <option key={idx} value={team} />
                        ))}
                    </datalist>
                </div>

                <button type="submit" style={{ marginTop: "1rem" }}>Zarezerwuj</button>
            </form>
            <p>{message}</p>
        </div>
    );
}
