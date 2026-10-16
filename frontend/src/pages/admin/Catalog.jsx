import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import Pagination from '../../components/Pagination';

const PAGE_SIZE = 20;

function useCrud(loadFn, createFn, updateFn, deleteFn) {
  const [list, setList] = useState([]);
  const [name, setName] = useState('');
  const [editing, setEditing] = useState(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [msg, setMsg] = useState('');

  const load = (p) => loadFn(p ?? page, PAGE_SIZE)
    .then((res) => {
      if (Array.isArray(res)) {
        setList(res);
        setTotal(res.length);
        setTotalPages(1);
      } else {
        setList(res.items || []);
        setTotal(res.total || 0);
        setTotalPages(res.totalPages || 1);
      }
    })
    .catch(console.error);
  useEffect(() => { load(1); }, []);

  const goPage = (n) => { setPage(n); load(n); window.scrollTo(0, 0); };

  const submit = async (e) => {
    e.preventDefault();
    setMsg('');
    try {
      if (editing) await updateFn(editing, name);
      else await createFn(name);
      setName('');
      setEditing(null);
      load();
    } catch (err) {
      setMsg(err.message);
    }
  };

  const remove = async (id) => {
    if (!confirm('Silinsin mi?')) return;
    try {
      await deleteFn(id);
      load();
    } catch (err) {
      setMsg(err.message);
    }
  };

  return { list, name, setName, editing, setEditing, msg, submit, remove, load, page, totalPages, total, goPage };
}

export function AdminCategories() {
  const c = useCrud(api.getCategories, api.createCategory, api.updateCategory, api.deleteCategory);
  return (
    <>
      <h2>Kategoriler ({c.total})</h2>
      {c.msg && <div className="error">{c.msg}</div>}
      <form onSubmit={c.submit} className="form inline-form">
        <input value={c.name} onChange={(e) => c.setName(e.target.value)} placeholder="Kategori adı" required />
        <button className="btn" type="submit">{c.editing ? 'Güncelle' : '+ Ekle'}</button>
        {c.editing && <button type="button" className="hbtn" onClick={() => { c.setEditing(null); c.setName(''); }}>Vazgeç</button>}
      </form>
      <div className="table-wrap">
        <table className="table">
          <tbody>
            {c.list.map((x) => (
              <tr key={x.id}>
                <td><strong>{x.name}</strong> <span className="muted">({x.productCount} ürün)</span></td>
                <td className="right">
                  <button className="hbtn" onClick={() => { c.setEditing(x.id); c.setName(x.name); }}>Düzenle</button>{' '}
                  <button className="hbtn" onClick={() => c.remove(x.id)}>Sil</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={c.page} totalPages={c.totalPages} onChange={c.goPage} />
    </>
  );
}

export function AdminBrands() {
  const b = useCrud(api.getBrands, api.createBrand, api.updateBrand, api.deleteBrand);
  return (
    <>
      <h2>Markalar ({b.total})</h2>
      {b.msg && <div className="error">{b.msg}</div>}
      <form onSubmit={b.submit} className="form inline-form">
        <input value={b.name} onChange={(e) => b.setName(e.target.value)} placeholder="Marka adı" required />
        <button className="btn" type="submit">{b.editing ? 'Güncelle' : '+ Ekle'}</button>
        {b.editing && <button type="button" className="hbtn" onClick={() => { b.setEditing(null); b.setName(''); }}>Vazgeç</button>}
      </form>
      <div className="table-wrap">
        <table className="table">
          <tbody>
            {b.list.map((x) => (
              <tr key={x.id}>
                <td><strong>{x.name}</strong> <span className="muted">({x.productCount} ürün)</span></td>
                <td className="right">
                  <button className="hbtn" onClick={() => { b.setEditing(x.id); b.setName(x.name); }}>Düzenle</button>{' '}
                  <button className="hbtn" onClick={() => b.remove(x.id)}>Sil</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={b.page} totalPages={b.totalPages} onChange={b.goPage} />
    </>
  );
}
