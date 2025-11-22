import styles from '../styles/components/nav.module.css';
import NavButton from './navButton';

function Nav() {
    return (
        <aside className={styles.sidebar}>
            <h2>Menu</h2>
            <ul>
                <li><NavButton name="Tournaments" /></li>
                <li><NavButton name="Team" /></li>
                <li><NavButton name="Top Players/Teams" /></li>
            </ul>
        </aside>
    );
}

export default Nav;