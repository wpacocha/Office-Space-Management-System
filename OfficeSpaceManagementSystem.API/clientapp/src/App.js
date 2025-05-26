import { BrowserRouter, Routes, Route } from "react-router-dom";
import ReservationForm from "./pages/ReservationForm";
import ReservationList from "./pages/ReservationList";
import Availability from "./pages/Availability";
import AdminPanel from './pages/AdminPanel';
import Navbar from "./components/Navbar";
import { useState } from "react";

function App() {
    const [darkMode, setDarkMode] = useState(false);

    return (
        <BrowserRouter>
            <div className={darkMode ? 'dark' : ''}>
                <div className="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 transition-colors">
                    <Navbar toggleDarkMode={() => setDarkMode(!darkMode)} darkMode={darkMode} />

                    <main className="p-8">
                        <Routes>
                            <Route path="/" element={<ReservationForm />} />
                            <Route path="/history" element={<ReservationList />} />
                            <Route path="/availability" element={<Availability />} /> 
                            <Route path="/admin" element={<AdminPanel />} />
                        </Routes>
                    </main>
                </div>
            </div>
        </BrowserRouter>
    );
}

export default App;
