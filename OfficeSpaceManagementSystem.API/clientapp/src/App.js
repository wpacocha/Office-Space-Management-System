import { useEffect, useState } from "react";

function App() {
    const [message, setMessage] = useState("Loading...");

    useEffect(() => {
        fetch("/validate")
            .then((res) => res.text())
            .then(setMessage)
            .catch((err) => setMessage("❌ Błąd połączenia z backendem"));
    }, []);

    return (
        <div style={{ padding: "2rem" }}>
            <h1>Office Space App</h1>
            <p>{message}</p>
        </div>
    );
}

export default App;
