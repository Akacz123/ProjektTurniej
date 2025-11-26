import React, { useEffect, useState } from 'react';
import MainPageContent from './mainPageContent'; 

const TournamentList = () => {
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('https://projektturniej.onrender.com/api/Tournaments')
      .then((res) => res.json())
      .then((data) => {
        setTournaments(data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Błąd:", err);
        setLoading(false);
      });
  }, []);

  if (loading) return <p style={{color: 'white', textAlign: 'center'}}>Ładowanie turniejów...</p>;

  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '20px', justifyContent: 'center', width: '100%' }}>
      
      {tournaments.map((t) => (
        <MainPageContent
          key={t.tournamentId} // Unikalne ID
          
          // --- TŁUMACZENIE JSON Z BACKENDU NA PROPSY KOMPONENTU ---
          title={t.tournamentName}
          description={t.description}
          baner={t.imageUrl}
          startDate={t.startDate}
          endDate={t.endDate}
          
          // Backend nie ma "location", więc używamy nazwy gry:
          location={t.game ? t.game.gameName : "Online"} 
          
          maxParticipants={t.maxParticipants}
          registrationType={t.registrationType}
          tournamentType={t.tournamentFormat}
          rules={t.rules}
        />
      ))}
      
    </div>
  );
};

export default TournamentList;