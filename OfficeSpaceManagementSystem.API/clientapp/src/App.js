import { useEffect, useState } from "react";
import axios from "axios";

function App() {
    const [zones, setZones] = useState([]);

    useEffect(() => {
        axios.get("/api/zones") // przez proxy trafi na https://localhost:5001/api/zones
            .then(res => setZones(res.data))
            .catch(err => console.error(err));
    }, []);

    return (
        <div>
            <h1>Strefy w biurze</h1>
            <ul>
                {zones.map(zone => (
                    <li key={zone.id}>{zone.name}</li>
                ))}
            </ul>
        </div>
    );
}

export default App;
