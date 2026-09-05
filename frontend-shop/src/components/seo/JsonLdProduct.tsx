import React from "react";
import { ShopProductDetail } from "@/types/product";

interface JsonLdProductProps {
  product: ShopProductDetail;
}

export default function JsonLdProduct({ product }: JsonLdProductProps) {
  const defaultVariant = product.variants[0];
  const defaultPrice =
    defaultVariant?.prices.find((p) => p.isDefault) ||
    defaultVariant?.prices[0];
  const priceValue = defaultPrice ? defaultPrice.discountedPrice : 0;

  const schema = {
    "@context": "https://schema.org/",
    "@type": "Product",
    name: product.name,
    image: product.imagePath ? [product.imagePath] : [],
    description:
      product.description ||
      `Mua ${product.name} tươi sạch tại Solaris ERP Farm.`,
    sku: product.code,
    brand: {
      "@type": "Brand",
      name: "Solaris Farm",
    },
    offers: {
      "@type": "Offer",
      url: `https://solaris.vn/san-pham/${product.slug}`,
      priceCurrency: "VND",
      price: priceValue,
      availability: defaultVariant?.isInStock
        ? "https://schema.org/InStock"
        : "https://schema.org/OutOfStock",
      priceValidUntil: "2026-12-31",
    },
    additionalProperty: Object.entries(product.attributes).map(
      ([key, val]) => ({
        "@type": "PropertyValue",
        name: key,
        value: val,
      }),
    ),
  };

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}
