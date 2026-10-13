import Header from './Header';

export default function Layout({ sidebar, children }) {
  return (
    <div className="app">
      <Header />
      <div className="main">
        {sidebar}
        <main className="content">{children}</main>
      </div>
    </div>
  );
}
