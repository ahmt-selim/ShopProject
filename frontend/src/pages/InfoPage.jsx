import { Link, useParams } from 'react-router-dom';
import Layout from '../components/Layout';
import { infoPages } from '../config/infoPages';

export default function InfoPage() {
  const { slug } = useParams();
  const page = infoPages[slug];

  if (!page) {
    return (
      <Layout>
        <h2>Sayfa bulunamadı</h2>
        <p>Aradığınız içerik yayında değil. <Link to="/">Anasayfaya dön</Link></p>
      </Layout>
    );
  }

  return (
    <Layout>
      <h2>{page.title}</h2>
      {page.blocks.map((b, i) => (
        <section key={i}>
          {b.h && <h3>{b.h}</h3>}
          {(b.ps || []).map((p, j) => <p key={j}>{p}</p>)}
          {b.ul && (
            <ul>
              {b.ul.map((item, k) => <li key={k}>{item}</li>)}
            </ul>
          )}
        </section>
      ))}
    </Layout>
  );
}
