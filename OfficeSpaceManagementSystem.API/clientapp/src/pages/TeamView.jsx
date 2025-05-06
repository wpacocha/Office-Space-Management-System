import React from 'react';

const mockTeam = [
    { name: 'Anna Nowak', email: 'anna@firma.pl' },
    { name: 'Tomasz Kowalski', email: 'tomasz@firma.pl' },
    { name: 'Ewa Zieli�ska', email: 'ewa@firma.pl' },
];

function TeamView() {
    return (
        <div>
            <h2>M�j Zesp�</h2>
            <ul>
                {mockTeam.map((member, index) => (
                    <li key={index}>
                        <strong>{member.name}</strong> � {member.email}
                    </li>
                ))}
            </ul>
        </div>
    );
}

export default TeamView;
