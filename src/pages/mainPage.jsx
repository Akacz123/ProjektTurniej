import Nav from "../components/nav";
import TitleBar from "../components/titleBar";
import MainPageContent from "../components/mainPageContent";
import styles from "../styles/pages/mainPage.module.css";
import TournamentList from "../components/TournamentList";


function MainPage() {
    return (
        <>
            <TitleBar />
            <div className={styles.mainContent}>
                <Nav  />
                <div  className={styles.container}> 
                    <TournamentList />
                </div>
            </div>
        </>
    );
}

export default MainPage;