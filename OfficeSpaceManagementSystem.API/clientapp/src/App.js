import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import ReservationForm from './pages/ReservationForm';
import ReservationList from './pages/ReservationList';


function App() {
    return (
        <BrowserRouter>
            <div>
                <h1>Office Space App</h1>
                <nav>
                    <Link to="/">Formularz</Link> |{" "}
                    <Link to="/reservations">Rezerwacje</Link> |{" "}
                </nav>

                <Routes>
                    <Route path="/" element={<ReservationForm />} />
                    <Route path="/reservations" element={<ReservationList />} />
                </Routes>
            </div>
        </BrowserRouter>
    );
}

export default App;
