import { useEffect, useState } from 'react';
import styles from '../styles/components/mainPageContent.module.css';
import Modal from './modal';   
import Button from './Button';

function MainPageContent(props) {
    const {
        title,
        description,
        baner,
        startDate,
        endDate,
        location,
        rules,
        maxParticipants,
        registrationType,
        tournamentType,
    } = props;

    const [state, setState] = useState("Upcoming");
    const [timeInfo, setTimeInfo] = useState("");
    const [isModalOpen, setIsModalOpen] = useState(false);

    // --- LOGIKA DAT ---
    const safeEndDate = endDate ? endDate : startDate;

    const handleOpenModal = () => setIsModalOpen(true);
    const handleCloseModal = () => setIsModalOpen(false);

    useEffect(() => {
        if (!startDate) return;
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const tournamentStartDate = new Date(startDate);
        const tournamentEndDate = new Date(safeEndDate);

        if (tournamentStartDate > today) {
            setState("Upcoming");
        } else if (tournamentEndDate < today) {
            setState("Completed");
        } else {
            setState("Ongoing");
        }
    }, [startDate, safeEndDate]);

    useEffect(() => {
        if (!startDate) return;
        const today = new Date();

        if (state === "Upcoming") {
            const tournamentStartDate = new Date(startDate);
            const diffTime = tournamentStartDate - today;
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

            if (diffDays <= 0) setTimeInfo("Launches Today");
            else if (diffDays === 1) setTimeInfo("Launches Tomorrow");
            else if (diffDays <= 31) setTimeInfo(`Launches in ${diffDays} days`);
            else setTimeInfo(`Launches at ${new Date(startDate).toLocaleDateString()}`);
        }
        else if (state === "Ongoing") {
            const tournamentEndDate = new Date(safeEndDate);
            const diffTime = tournamentEndDate - today;
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

            if (diffDays <= 0) setTimeInfo("Ends Today");
            else if (diffDays === 1) setTimeInfo("Ends Tomorrow");
            else setTimeInfo(`Ends in ${diffDays} days`);
        }
        else if (state === "Completed") {
            const tournamentEndDate = new Date(safeEndDate);
            const diffTime = today - tournamentEndDate;
            const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));

            if (diffDays <= 0) setTimeInfo("Ended Today");
            else if (diffDays === 1) setTimeInfo("Ended 1 day ago");
            else if (diffDays <= 31) setTimeInfo(`Ended ${diffDays} days ago`);
            else setTimeInfo(`Ended at ${new Date(safeEndDate).toLocaleDateString()}`);
        }
    }, [state, startDate, safeEndDate]);


    return (
        <>        
            <div className={styles.container} onClick={handleOpenModal}>
                
                <div className={styles.bannerWrapper}>
                    <img 
                        src={baner || 'https://placehold.co/600x400?text=No+Image'} 
                        alt="Tournament Banner" 
                        className={styles.banner} 
                    />
                </div>

                <div className={styles.contentWrapper}>
                    <h3 className={styles.title}>{title}</h3>

                    <div className={styles.timeInfo}>{timeInfo}</div>

                    <ul className={styles.list}>
                        <li><strong>Location:</strong> <span>{location}</span></li>
                        <li><strong>Max Participants:</strong> <span>{maxParticipants}</span></li>
                        <li><strong>Registration Type:</strong> <span>{registrationType}</span></li>
                        <li><strong>State:</strong> <span>{state}</span></li>
                    </ul>
                </div>
            </div>

            {isModalOpen && (
                <Modal onClose={handleCloseModal}>
                    <h2 className={styles.modalTitle}>{title}</h2>
                    <img 
                        src={baner || 'https://placehold.co/600x400?text=No+Image'} 
                        alt="Tournament Banner" 
                        className={styles.modalBanner} 
                    />
                    <p className={styles.modalDescription}>{description || "Brak opisu."}</p>

                    <ul className={styles.modalList}>
                        <li><strong>Localization:</strong> <span>{location}</span></li>
                        <li><strong>Start Date:</strong> <span>{new Date(startDate).toLocaleDateString()}</span></li>
                        <li><strong>End Date:</strong> <span>{endDate ? new Date(endDate).toLocaleDateString() : "TBA"}</span></li>
                        <li><strong>Rules:</strong> <span>{rules || "Standard rules apply."}</span></li>
                        <li><strong>Max Participants:</strong> <span>{maxParticipants}</span></li>
                        <li><strong>Registration Type:</strong> <span>{registrationType}</span></li>
                        <li><strong>Tournament Type:</strong> <span>{tournamentType}</span></li>
                        <li><strong>State:</strong> <span>{state}</span></li>
                    </ul>

                    <div className={styles.modalActions}>
                        <Button name="registration" className={styles.registrationButton} />
                    </div>
                </Modal>
            )}
        </>
    );
}

export default MainPageContent;