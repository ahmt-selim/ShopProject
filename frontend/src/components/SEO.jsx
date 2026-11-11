/* eslint-disable react/prop-types */
import { Helmet } from 'react-helmet-async';

export default function SEO({ title, description, canonical, jsonLd, noindex }) {
  const fullTitle = title ? `${title} | ihtiyacın3D` : 'ihtiyacın3D - 3D Baskı ve İhtiyaç Ürünleri';
  const desc = (description || '').slice(0, 155);
  return (
    <Helmet>
      <title>{fullTitle.slice(0, 60)}</title>
      <meta name="description" content={desc} />
      {canonical && <link rel="canonical" href={canonical} />}
      {noindex && <meta name="robots" content="noindex, follow" />}
      {jsonLd && (
        <script type="application/ld+json">
          {JSON.stringify(jsonLd)}
        </script>
      )}
    </Helmet>
  );
}