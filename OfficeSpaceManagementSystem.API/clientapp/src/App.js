import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import ReservationForm from './pages/ReservationForm';
import ReservationList from './pages/ReservationList';
import TeamView from './pages/TeamView';
import StatisticsView from './pages/StatisticsView';

function App() {
    return (
        <BrowserRouter>
            <div>
                <h1>Office Space App</h1>
                <nav>
                    <Link to="/">Formularz</Link> |{" "}
                    <Link to="/reservations">Rezerwacje</Link> |{" "}
                    <Link to="/team">Zespół</Link> |{" "}
                    <Link to="/stats">Statystyki</Link>
                </nav>

                <Routes>
                    <Route path="/" element={<ReservationForm />} />
                    <Route path="/reservations" element={<ReservationList />} />
                    <Route path="/team" element={<TeamView />} />
                    <Route path="/stats" element={<StatisticsView />} />
                </Routes>
            </div>
        </BrowserRouter>
    );
}

export default App;
