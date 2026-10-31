import { useEffect, useState } from 'react';
import BookingPage from './pages/BookingPage';
import CancelPage from './pages/CancelPage';
import AdminPage from './pages/AdminPage';

type Page = 'foglalas' | 'lemondas' | 'muhely';

const pages: { id: Page; label: string }[] = [
  { id: 'foglalas', label: 'Időpontfoglalás' },
  { id: 'lemondas', label: 'Lemondás' },
  { id: 'muhely', label: 'Műhely' },
];

// Tiny hash router: #/foglalas, #/lemondas, #/muhely. Works without server configuration.
function readPage(): Page {
  const id = window.location.hash.replace('#/', '');
  return pages.some((p) => p.id === id) ? (id as Page) : 'foglalas';
}

export default function App() {
  const [page, setPage] = useState<Page>(readPage);

  useEffect(() => {
    const onHashChange = () => setPage(readPage());
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  return (
    <div className="shell">
      <header className="topbar">
        <a className="brand" href="#/foglalas">
          <span className="brand-mark" aria-hidden="true" />
          Emelő Autószerviz
        </a>
        <nav aria-label="Fő menü">
          <ul className="nav">
            {pages.map((p) => (
              <li key={p.id}>
                <a href={`#/${p.id}`} aria-current={page === p.id ? 'page' : undefined}>
                  {p.label}
                </a>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      <main className="content">
        {page === 'foglalas' && <BookingPage />}
        {page === 'lemondas' && <CancelPage />}
        {page === 'muhely' && <AdminPage />}
      </main>

      <footer className="footer">
        Hétköznap 8:00 és 17:00 között dolgozunk. Két emelőnk van, így egyszerre két autóval foglalkozunk.
      </footer>
    </div>
  );
}
