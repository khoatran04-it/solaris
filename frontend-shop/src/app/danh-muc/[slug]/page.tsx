import React from "react";
import { Metadata } from "next";
import Link from "next/link";
import { Layers, Sparkles } from "lucide-react";
import ProductCard from "@/components/product/ProductCard";
import ProductFilter from "@/components/product/ProductFilter";
import Pagination from "@/components/common/Pagination";
import EmptyState from "@/components/common/EmptyState";
import shopProductApi from "@/api/shopProductApi";
import { PagedResult } from "@/types/common";
import {
  ShopProductCard,
  ShopCategoryTree,
  ShopProductFilterParams,
} from "@/types/product";

interface CategoryPageProps {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{
    page?: string;
    product?: string;
    origin?: string;
    cert?: string;
    sort?: string;
  }>;
}

export async function generateMetadata({
  params,
}: CategoryPageProps): Promise<Metadata> {
  const { slug } = await params;
  try {
    const categories = await shopProductApi.getCategories();
    const matchedGroup = categories.find((g) => g.groupSlug === slug);
    if (matchedGroup) {
      return {
        title: `${matchedGroup.groupName} - Nông Sản Sạch Chuẩn VietGAP`,
        description: `Mua sắm các sản phẩm ${matchedGroup.groupName} tươi sạch đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
      };
    }
    for (const g of categories) {
      const matchedCat = g.categories.find((c) => c.categorySlug === slug);
      if (matchedCat) {
        return {
          title: `${matchedCat.categoryName} - Nông Sản Sạch Chuẩn VietGAP`,
          description: `Mua sắm ${matchedCat.categoryName} tươi ngon đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
        };
      }
    }
  } catch {}
  return { title: "Danh Mục Nông Sản Sạch" };
}

export default async function CategoryPage({
  params,
  searchParams,
}: CategoryPageProps) {
  const { slug } = await params;
  const sParams = await searchParams;
  const page = parseInt(sParams.page || "1", 10);
  const origin = sParams.origin || "";
  const cert = sParams.cert || "";
  const sort = sParams.sort || "newest";

  let categories: ShopCategoryTree[] = [];
  let origins: string[] = [];
  let certifications: string[] = [];

  try {
    const [catRes, origRes, certRes] = await Promise.all([
      shopProductApi.getCategories().catch(() => []),
      shopProductApi.getOrigins().catch(() => []),
      shopProductApi.getCertifications().catch(() => []),
    ]);
    categories = catRes;
    origins = origRes;
    certifications = certRes;
  } catch {}

  // Phân giải slug: GroupSlug hoặc CategorySlug hoặc ProductSlug hoặc ID
  let title = "Danh Mục Nông Sản";
  let groupSlug: string | undefined = undefined;
  let categorySlug: string | undefined = undefined;
  let productSlug: string | undefined = sParams.product || undefined;
  let matchedGroupName = "";
  let matchedCategoryName = "";

  const matchedGroup = categories.find(
    (g) => g.groupSlug === slug || g.groupId?.toString() === slug,
  );
  if (matchedGroup) {
    title = matchedGroup.groupName;
    groupSlug = matchedGroup.groupSlug || slug;
  } else {
    for (const g of categories) {
      const matchedCat = g.categories.find(
        (c) => c.categorySlug === slug || c.categoryId?.toString() === slug,
      );
      if (matchedCat) {
        title = matchedCat.categoryName;
        matchedGroupName = g.groupName;
        categorySlug = matchedCat.categorySlug || slug;
        break;
      }
    }

    // Nếu không khớp category, kiểm tra xem có khớp product slug trực tiếp không
    if (!categorySlug) {
      for (const g of categories) {
        for (const c of g.categories) {
          const matchedProd = c.products?.find(
            (p) => p.productSlug === slug || p.productId?.toString() === slug,
          );
          if (matchedProd) {
            title = matchedProd.productName;
            matchedGroupName = g.groupName;
            matchedCategoryName = c.categoryName;
            categorySlug = c.categorySlug;
            productSlug = matchedProd.productSlug || slug;
            break;
          }
        }
        if (productSlug) break;
      }
    }

    // Robust Fallback: Nếu không match trong tree, gửi slug trực tiếp làm categorySlug
    if (!categorySlug && !groupSlug && !productSlug) {
      categorySlug = slug;
    }
  }

  // Nếu có sParams.product qua query string, tìm tên sản phẩm để cập nhật title/breadcrumb
  if (sParams.product) {
    productSlug = sParams.product;
    for (const g of categories) {
      for (const c of g.categories) {
        const matchedProd = c.products?.find((p) => p.productSlug === productSlug);
        if (matchedProd) {
          title = matchedProd.productName;
          if (!matchedGroupName) matchedGroupName = g.groupName;
          if (!matchedCategoryName) matchedCategoryName = c.categoryName;
          break;
        }
      }
      if (matchedCategoryName) break;
    }
  }

  const filterParams: ShopProductFilterParams = {
    pageIndex: page,
    pageSize: 12,
    categoryGroupSlug: groupSlug,
    categorySlug: categorySlug,
    productSlug: productSlug,
    origin: origin || undefined,
    certification: cert || undefined,
    sortBy: sort,
  };

  let productsResult: PagedResult<ShopProductCard> = {
    items: [],
    totalRecords: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 12,
  };

  try {
    productsResult = await shopProductApi.getAll(filterParams);
  } catch {}

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      {/* 1. Header Banner & Breadcrumb */}
      <div className="bg-gradient-to-r from-emerald-800 via-emerald-700 to-teal-800 rounded-3xl p-6 sm:p-8 text-white shadow-lg relative overflow-hidden">
        <div className="absolute right-0 top-0 w-80 h-80 bg-white/5 rounded-full blur-3xl pointer-events-none" />
        <div className="relative z-10 space-y-3 max-w-3xl">
          <nav className="flex items-center gap-2 text-xs text-emerald-200/90 font-medium">
            <Link href="/" className="hover:text-white transition-colors">
              Trang chủ
            </Link>
            <span>/</span>
            <Link
              href="/san-pham"
              className="hover:text-white transition-colors"
            >
              Sản phẩm
            </Link>
            {matchedGroupName && (
              <>
                <span>/</span>
                <span className="text-emerald-100">{matchedGroupName}</span>
              </>
            )}
            {matchedCategoryName && (
              <>
                <span>/</span>
                <span className="text-emerald-100">{matchedCategoryName}</span>
              </>
            )}
            <span>/</span>
            <span className="font-bold text-white">{title}</span>
          </nav>

          <div className="space-y-1">
            <h1 className="text-2xl sm:text-3xl font-black tracking-tight">
              {title}
            </h1>
            <p className="text-xs text-emerald-100/90 leading-relaxed font-normal">
              Tuyển chọn các mặt hàng {title.toLowerCase()} tươi ngon mỗi ngày,
              đạt tiêu chuẩn kiểm soát chất lượng FEFO và nguồn gốc vùng trồng
              minh bạch.
            </p>
          </div>

          <div className="flex items-center gap-3 pt-1">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-white/15 rounded-full text-[11px] font-bold text-white backdrop-blur-xs">
              <Sparkles className="w-3.5 h-3.5 text-amber-300" />
              {productsResult.totalRecords} sản phẩm sẵn có
            </span>
          </div>
        </div>
      </div>

      {/* 2. Layout: Sidebar Filter + Product Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
        {/* Left Sidebar Filter */}
        <div className="lg:col-span-1 sticky top-28">
          <ProductFilter
            categories={categories}
            origins={origins}
            certifications={certifications}
          />
        </div>

        {/* Right Product Grid */}
        <div className="lg:col-span-3 space-y-8">
          {productsResult.items.length > 0 ? (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
                {productsResult.items.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>

              {/* Standardized Pagination Controls */}
              <Pagination
                currentPage={productsResult.currentPage}
                totalPages={productsResult.totalPages}
                baseUrl={`/danh-muc/${slug}`}
                searchParams={sParams as any}
              />
            </>
          ) : (
            <EmptyState
              icon={<Layers className="w-7 h-7" />}
              title="Chưa có sản phẩm trong danh mục này"
              description="Vui lòng quay lại sau hoặc tham khảo các danh mục nông sản tươi sạch khác của chúng tôi."
              actionText="Khám phá toàn bộ sản phẩm"
              actionHref="/san-pham"
            />
          )}
        </div>
      </div>
    </div>
  );
}
