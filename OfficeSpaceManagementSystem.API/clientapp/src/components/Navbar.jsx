import { Link } from 'react-router-dom';

export default function Navbar({ toggleDarkMode, darkMode }) {
    return (
        <nav className="bg-white dark:bg-gray-800 p-4 shadow-md flex justify-between items-center">
            <div className="flex space-x-6">
                <Link to="/" className="text-lg font-semibold hover:text-primary transition-colors">
                    Reserve Desk
                </Link>
                <Link to="/history" className="text-lg font-semibold hover:text-primary transition-colors">
                    My Reservations
                </Link>
                <Link to="/availability" className="text-lg font-semibold hover:text-primary transition-colors">
                    Check Availability
                </Link>
            </div>
            <button
                onClick={toggleDarkMode}
                className="bg-primary text-white px-4 py-2 rounded-lg hover:bg-purple-700 transition-colors"
            >
                {darkMode ? "Light Mode" : "Dark Mode"}
            </button>
        </nav>
    );
}
