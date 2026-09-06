import React from "react";
import { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import ProductDetailClient from "@/components/product/ProductDetailClient";
import JsonLdProduct from "@/components/seo/JsonLdProduct";
import shopProductApi from "@/api/shopProductApi";
import { ShopProductDetail } from "@/types/product";

interface ProductDetailPageProps {
  params: Promise<{
    slug: string;
  }>;
}

// Generate dynamic SEO Metadata for SEOQuake
export async function generateMetadata({
  params,
}: ProductDetailPageProps): Promise<Metadata> {
  const { slug } = await params;
  try {
    const product = await shopProductApi.getBySlug(slug);
    if (!product) return { title: "Sản phẩm không tồn tại" };

    const origin =
      product.attributes["Xuất xứ / Vùng trồng"] ||
      product.attributes["Xuất xứ"] ||
      "Việt Nam";
    const cert =
      product.attributes["Chứng nhận chất lượng"] ||
      product.attributes["Chứng nhận"] ||
      "VietGAP";

    return {
      title: `${product.name} - Chuẩn ${cert}, Xuất xứ ${origin}`,
      description:
        product.description ||
        `Mua ${product.name} tươi ngon tại Solaris Farm. Đạt chuẩn ${cert}, nguồn gốc ${origin}, giao hàng nhanh 2h.`,
      openGraph: {
        title: `${product.name} | Solaris Nông Sản Sạch`,
        description: `Mua ${product.name} chuẩn VietGAP/GlobalGAP giao tươi 2h.`,
        images: product.imagePath ? [product.imagePath] : [],
        type: "article",
      },
      alternates: {
        canonical: `https://solaris.vn/san-pham/${product.slug}`,
      },
    };
  } catch {
    return { title: "Chi tiết sản phẩm" };
  }
}

export default async function ProductDetailPage({
  params,
}: ProductDetailPageProps) {
  const { slug } = await params;

  let product: ShopProductDetail | null = null;
  try {
    product = await shopProductApi.getBySlug(slug);
  } catch {
    notFound();
  }

  if (!product) {
    notFound();
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-10">
      {/* JSON-LD Schema Structured Data */}
      <JsonLdProduct product={product} />

      {/* Breadcrumb Navigation */}
      <nav className="flex items-center gap-2 text-xs text-slate-500 font-medium">
        <Link href="/" className="hover:text-emerald-700 transition-colors">
          Trang chủ
        </Link>
        <span>/</span>
        <Link
          href="/san-pham"
          className="hover:text-emerald-700 transition-colors"
        >
          Sản phẩm
        </Link>
        {product.categoryName && (
          <>
            <span>/</span>
            <Link
              href={`/danh-muc/${product.categorySlug}`}
              className="hover:text-emerald-700 transition-colors"
            >
              {product.categoryName}
            </Link>
          </>
        )}
        <span>/</span>
        <span className="font-bold text-slate-900 truncate max-w-xs">
          {product.name}
        </span>
      </nav>

      {/* Full Interactive Product Experience (Images, SKUs, Variants, Descriptions & Specs) */}
      <ProductDetailClient product={product} />
    </div>
  );
}
