import React, { useEffect, useState } from 'react';
import TournamentList from "./components/TournamentList"; 
import MainPage from './pages/mainPage';
import LogIn from './pages/logIn.jsx';
import SignUp from './pages/singUp.jsx';
import { Routes, Route } from 'react-router-dom';

function App() {

  return (
    <>
      {/* Kontener na wszystkie ścieżki */}
      <Routes>
        
        {/* Ścieżka główna (localhost:3000/) */}
        <Route path="/" element={<MainPage />} />
        
        {/* Logowanie (localhost:3000/login) */}
        <Route path="/login" element={<LogIn />} />
        
        {/* Rejestracja (localhost:3000/signup) */}
        <Route path="/signup" element={<SignUp />} />
        
      </Routes>
    </>
  );
}

export default App;