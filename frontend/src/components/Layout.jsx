/* eslint-disable react/prop-types -- proje genelinde propTypes kullanılmıyor (düz JSX) */
import Header from './Header';
import Footer from './Footer';

export default function Layout({ sidebar, children }) {
  return (
    <div className="app">
      <Header />
      <div className="main">
        {sidebar}
        <main className="content">{children}</main>
      </div>
      <Footer />
    </div>
  );
}
