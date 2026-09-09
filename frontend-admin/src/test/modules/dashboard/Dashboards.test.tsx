import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import OverviewDashboard from '../../../pages/dashboard/OverviewDashboard';
import SalesGeographyDashboard from '../../../pages/dashboard/SalesGeographyDashboard';
import InventoryCapacityDashboard from '../../../pages/dashboard/InventoryCapacityDashboard';
import QualityExpiryDashboard from '../../../pages/dashboard/QualityExpiryDashboard';
import FinancialPerformanceDashboard from '../../../pages/dashboard/FinancialPerformanceDashboard';
import PriceVolatilityDashboard from '../../../pages/dashboard/PriceVolatilityDashboard';
import { dashboardApi } from '../../../api/dashboardApi';

// Mock API
vi.mock('../../../api/dashboardApi', () => ({
  dashboardApi: {
    getOverview: vi.fn(),
    getSalesGeography: vi.fn(),
    getInventoryCapacity: vi.fn(),
    getQualityExpiry: vi.fn(),
    getFinancialPerformance: vi.fn(),
    getPriceVolatility: vi.fn(),
    getPriceVolatilitySkus: vi.fn(),
  },
}));

describe('Module 16 - Executive Dashboards', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ==========================================================================
  // DASHBOARD 1: OVERVIEW DASHBOARD
  // ==========================================================================
  describe('Dashboard 1: OverviewDashboard', () => {
    const mockOverviewData = {
      totalRevenue: 50000000,
      revenueGrowthPercent: 12.5,
      totalOrders: 150,
      completedOrders: 135,
      averageOrderValue: 370370,
      fulfillmentRatePercent: 90.0,
      revenueTimeline: [
        { label: '01/09', revenue: 10000000, orderCount: 30 },
        { label: '02/09', revenue: 15000000, orderCount: 45 },
      ],
      paymentMethodBreakdown: [
        { method: 'COD', count: 80, amount: 25000000, percent: 50.0 },
        { method: 'VNPay', count: 55, amount: 25000000, percent: 50.0 },
      ],
      orderStatusPipeline: [
        { status: 'Completed', count: 135 },
        { status: 'Pending', count: 15 },
      ],
      recentOrders: [
        {
          id: 1,
          orderCode: 'ORD-2026-001',
          customerName: 'Nguyễn Văn A',
          totalAmount: 500000,
          status: 'Completed',
          orderDate: '2026-09-01T10:00:00Z',
        },
      ],
    };

    it('TC01 - Hiển thị đầy đủ KPI Cards, Biểu đồ & Đơn hàng gần nhất', async () => {
      (dashboardApi.getOverview as any).mockResolvedValue(mockOverviewData);

      render(
        <MemoryRouter>
          <OverviewDashboard />
        </MemoryRouter>
      );

      // Verify Header & KPIs
      expect(screen.getByText(/Bàn làm việc Điều hành/i)).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Tổng Doanh Thu')).toBeInTheDocument();
        expect(screen.getByText('Tổng Đơn Hàng')).toBeInTheDocument();
        expect(screen.getByText('Giá Trị TB Đơn')).toBeInTheDocument();
        expect(screen.getByText('Tỉ Lệ Hoàn Tất')).toBeInTheDocument();
        expect(screen.getByText('90%')).toBeInTheDocument();
      });

      // Verify Recent orders table
      expect(screen.getByText('ORD-2026-001')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();

      // Change period filter to "7 ngày"
      const btn7Days = screen.getByRole('button', { name: /7 ngày/i });
      fireEvent.click(btn7Days);

      await waitFor(() => {
        expect(dashboardApi.getOverview).toHaveBeenCalledWith('7days');
      });
    });
  });

  // ==========================================================================
  // DASHBOARD 2: SALES & GEOGRAPHY DASHBOARD
  // ==========================================================================
  describe('Dashboard 2: SalesGeographyDashboard', () => {
    const mockSalesData = {
      topProducts: [
        {
          variantId: 1,
          name: 'Xoài Cát Hòa Lộc 1kg',
          code: 'SKU-XOAI-01',
          uoM: 'Kg',
          quantitySold: 120,
          totalRevenue: 12000000,
          revenuePercent: 40.0,
        },
      ],
      categoryBreakdown: [
        {
          categoryGroupName: 'Trái cây đặc sản',
          totalRevenue: 18000000,
          quantitySold: 150,
          percent: 60.0,
        },
      ],
      geographyBreakdown: [
        {
          provinceName: 'Hà Nội',
          orderCount: 85,
          totalRevenue: 20000000,
          percent: 55.0,
        },
      ],
      customerTierBreakdown: [
        {
          tierName: 'VIP Kim Cương',
          customerCount: 10,
          orderCount: 30,
          totalRevenue: 15000000,
          percent: 30.0,
        },
      ],
    };

    it('TC02 - Render Top sản phẩm, Ngành hàng, Phân bổ Tỉnh/TP và Hạng khách', async () => {
      (dashboardApi.getSalesGeography as any).mockResolvedValue(mockSalesData);

      render(
        <MemoryRouter>
          <SalesGeographyDashboard />
        </MemoryRouter>
      );

      await waitFor(() => {
        expect(screen.getByText(/Doanh số & Khách hàng/i)).toBeInTheDocument();
        expect(screen.getAllByText('Xoài Cát Hòa Lộc 1kg')[0]).toBeInTheDocument();
        expect(screen.getByText('Hà Nội')).toBeInTheDocument();
        expect(screen.getAllByText('VIP Kim Cương')[0]).toBeInTheDocument();
      });
    });
  });

  // ==========================================================================
  // DASHBOARD 3: INVENTORY & CAPACITY DASHBOARD
  // ==========================================================================
  describe('Dashboard 3: InventoryCapacityDashboard', () => {
    const mockCapacityData = {
      totalStockValue: 120000000,
      totalActiveWarehouses: 2,
      warehouseCapacities: [
        {
          warehouseId: 1,
          warehouseName: 'Tổng Kho Hà Nội',
          warehouseCode: 'WH-HN-01',
          totalCbm: 500,
          occupiedCbm: 150,
          occupancyCbmPercent: 30.0,
          maxWeightKg: 100000,
          occupiedWeightKg: 20000,
          occupancyWeightPercent: 20.0,
          status: 'Safe' as const,
          totalAreaSqm: 1200,
          warningThresholdPercent: 85,
        },
      ],
      inventoryCompartments: {
        availableQty: 5000,
        reservedQty: 300,
        inQcQty: 200,
        damagedQty: 50,
        totalValue: 120000000,
      },
      topSpaceConsumingProducts: [
        {
          variantName: 'Gạo ST25 Bao 25kg',
          variantCode: 'GAO-ST25-25K',
          totalCbm: 15.5,
          totalQty: 200,
        },
      ],
      lowStockAlerts: [
        {
          variantName: 'Dâu Tây Đà Lạt 500g',
          variantCode: 'DAU-TAY-500G',
          warehouseName: 'Tổng Kho Hà Nội',
          availableQty: 5,
        },
      ],
    };

    it('TC03 - Render Thước đo Sức chứa Kho CBM/Tải trọng, Phân ngăn & Cảnh báo tồn thấp', async () => {
      (dashboardApi.getInventoryCapacity as any).mockResolvedValue(mockCapacityData);

      render(
        <MemoryRouter>
          <InventoryCapacityDashboard />
        </MemoryRouter>
      );

      await waitFor(() => {
        expect(screen.getByText(/Tồn kho & Sức chứa Kho hàng/i)).toBeInTheDocument();
        expect(screen.getAllByText('Tổng Kho Hà Nội')[0]).toBeInTheDocument();
        expect(screen.getByText('WH-HN-01')).toBeInTheDocument();
        expect(screen.getByText('Gạo ST25 Bao 25kg')).toBeInTheDocument();
        expect(screen.getByText('Dâu Tây Đà Lạt 500g')).toBeInTheDocument();
      });
    });
  });

  // ==========================================================================
  // DASHBOARD 4: QUALITY & EXPIRY DASHBOARD
  // ==========================================================================
  describe('Dashboard 4: QualityExpiryDashboard', () => {
    const mockQualityData = {
      expiryOverview: {
        expiredCount: 2,
        criticalCount: 5,
        warningCount: 8,
        safeCount: 80,
      },
      inboundQcRejectRatePercent: 4.5,
      totalInboundItems: 1000,
      totalRejectedItems: 45,
      customerReturnRatePercent: 1.2,
      totalOrders: 500,
      totalReturnOrders: 6,
      expiringBatches: [
        {
          batchCode: 'BATCH-DAUTAY-001',
          productName: 'Dâu Tây Mộc Châu',
          variantName: 'Hộp 500g',
          warehouseName: 'Kho Lạnh Hà Nội',
          expiryDate: '2026-09-08T00:00:00Z',
          daysRemaining: 3,
          quantityAvailable: 40,
          unitPrice: 85000,
          estimatedLossValue: 3400000,
        },
      ],
      qcRejectReasons: [{ reason: 'Trái cây dập nát', count: 30, percent: 66.7 }],
    };

    it('TC04 - Render Nhóm FEFO hạn sử dụng, Tỷ lệ Inbound QC & Danh sách lô hàng cận date', async () => {
      (dashboardApi.getQualityExpiry as any).mockResolvedValue(mockQualityData);

      render(
        <MemoryRouter>
          <QualityExpiryDashboard />
        </MemoryRouter>
      );

      await waitFor(() => {
        expect(screen.getByText(/Chất lượng & Hạn dùng \(FEFO\)/i)).toBeInTheDocument();
        expect(screen.getByText(/4.5%/i)).toBeInTheDocument();
        expect(screen.getByText(/1.2%/i)).toBeInTheDocument();
        expect(screen.getByText('BATCH-DAUTAY-001')).toBeInTheDocument();
        expect(screen.getByText('Dâu Tây Mộc Châu')).toBeInTheDocument();
        expect(screen.getByText('Trái cây dập nát')).toBeInTheDocument();
      });
    });
  });

  // ==========================================================================
  // DASHBOARD 5: FINANCIAL & CASH FLOW PERFORMANCE DASHBOARD
  // ==========================================================================
  describe('Dashboard 5: FinancialPerformanceDashboard', () => {
    const mockFinancialData = {
      grossRevenue: 105000000,
      customerRefunds: 5000000,
      netRevenue: 100000000,
      totalCogs: 65000000,
      grossProfit: 35000000,
      grossMarginPercent: 35.0,
      totalPoValue: 80000000,
      totalGoodsReceivedValue: 70000000,
      estimatedNetCashFlow: 25000000,
      netRevenueGrowthPercent: 15.0,
      cashFlowBridge: {
        grossSales: 105000000,
        onlinePaymentInflow: 60000000,
        codCollectedInflow: 35000000,
        codInTransitAmount: 10000000,
        customerRefundOutflow: 5000000,
        inboundGoodsReceiptOutflow: 70000000,
        pendingPoCommitment: 10000000,
        netOperatingCashFlow: 25000000,
      },
      timeline: [
        {
          label: '01/09',
          revenue: 20000000,
          cogs: 13000000,
          grossProfit: 7000000,
          cashInflow: 18000000,
          cashOutflow: 14000000,
        },
      ],
      supplierPayables: [
        {
          supplierId: 1,
          supplierName: 'Nông Trại Xanh',
          supplierCode: 'SUP-001',
          totalPoCount: 5,
          totalPoValue: 50000000,
          receivedValue: 45000000,
          qcRejectedValue: 2000000,
          pendingCommitment: 5000000,
        },
      ],
      categoryProfitability: [
        {
          categoryGroupName: 'Trái Cây Tươi',
          revenue: 60000000,
          cogs: 36000000,
          grossProfit: 24000000,
          grossMarginPercent: 40.0,
          quantitySold: 1200,
        },
      ],
      shrinkageLoss: {
        damagedStockValue: 1500000,
        expiringStockRiskValue: 3000000,
        returnRefundLoss: 5000000,
        totalShrinkageLoss: 9500000,
      },
    };

    it('TC05 - Render KPI Doanh thu, COGS, Lợi nhuận gộp, Cầu nối dòng tiền & Đối soát NCC', async () => {
      (dashboardApi.getFinancialPerformance as any).mockResolvedValue(mockFinancialData);

      render(
        <MemoryRouter>
          <FinancialPerformanceDashboard />
        </MemoryRouter>
      );

      // Verify Header
      expect(screen.getByText(/Dòng tiền & Đối soát Công nợ/i)).toBeInTheDocument();

      // Verify KPIs
      await waitFor(() => {
        expect(screen.getByText('Doanh thu thuần')).toBeInTheDocument();
        expect(screen.getByText('Giá vốn hàng bán (COGS)')).toBeInTheDocument();
        expect(screen.getByText('Lợi nhuận gộp')).toBeInTheDocument();
        expect(screen.getByText('Dòng tiền HĐKD ròng')).toBeInTheDocument();
      });

      // Verify Cash Flow Bridge
      expect(screen.getByText(/Dòng tiền vào \(Bán hàng\)/i)).toBeInTheDocument();
      expect(screen.getByText(/Nghĩa vụ chi trả \(Nhập hàng\)/i)).toBeInTheDocument();

      // Verify Supplier payables table
      expect(screen.getByText('Nông Trại Xanh')).toBeInTheDocument();
      expect(screen.getByText('SUP-001')).toBeInTheDocument();

      // Verify Category profitability table
      expect(screen.getByText('Trái Cây Tươi')).toBeInTheDocument();
      expect(screen.getByText('40%')).toBeInTheDocument();

      // Verify Shrinkage cards
      expect(screen.getByText('Hàng hỏng lưu kho')).toBeInTheDocument();
      expect(screen.getByText(/Rủi ro cận hạn/i)).toBeInTheDocument();
      expect(screen.getByText('Tổng hao hụt & rủi ro')).toBeInTheDocument();

      // Test period switcher
      const btn7Days = screen.getByRole('button', { name: /7 ngày/i });
      fireEvent.click(btn7Days);

      await waitFor(() => {
        expect(dashboardApi.getFinancialPerformance).toHaveBeenCalledWith('7days');
      });
    });
  });

  // ==========================================================================
  // DASHBOARD 6: PRICE VOLATILITY DASHBOARD
  // ==========================================================================
  describe('Dashboard 6: PriceVolatilityDashboard', () => {
    const mockSkus = [
      {
        variantId: 1,
        variantName: 'Xoài Cát Hòa Lộc Hộp 3kg',
        variantCode: 'SKU-XOAI-3KG',
        productName: 'Xoài Cát Hòa Lộc',
        baseUoMName: 'Kg',
      },
      {
        variantId: 2,
        variantName: 'Dâu Tây Mộc Châu 500g',
        variantCode: 'SKU-DAUTAY-500G',
        productName: 'Dâu Tây',
        baseUoMName: 'Hộp',
      },
    ];

    const mockVolatilityData = {
      variantId: 1,
      variantName: 'Xoài Cát Hòa Lộc Hộp 3kg',
      variantCode: 'SKU-XOAI-3KG',
      productName: 'Xoài Cát Hòa Lộc',
      baseUoMName: 'Kg',
      latestImportPrice: 40000,
      currentSellingPrice: 55000,
      priceSpread: 15000,
      marginPercent: 27.3,
      importPriceChangePercent: 5.2,
      isLossMaking: false,
      timeline: [
        {
          label: 'T07/2026',
          avgImportPrice: 38000,
          avgSellingPrice: 54000,
          spread: 16000,
          marginPercent: 29.6,
          isProfit: true,
        },
        {
          label: 'T08/2026',
          avgImportPrice: 40000,
          avgSellingPrice: 55000,
          spread: 15000,
          marginPercent: 27.3,
          isProfit: true,
        },
      ],
      transactions: [
        {
          date: '2026-09-01T10:00:00Z',
          type: 'Nhập hàng',
          documentCode: 'PO-2026-001',
          partnerName: 'Hợp Tác Xã Xoài Tiền Giang',
          originalUnitPrice: 2000000,
          originalUoMName: 'Sọt 50kg',
          normalizedUnitPrice: 40000,
          quantityInBaseUoM: 500,
          baseUoMName: 'Kg',
        },
      ],
    };

    it('TC06 - Render Biểu đồ biến động giá, Thẻ Spread lãi lỗ và chuyển đổi bộ lọc Tuần/Tháng/Năm', async () => {
      (dashboardApi.getPriceVolatilitySkus as any).mockResolvedValue(mockSkus);
      (dashboardApi.getPriceVolatility as any).mockResolvedValue(mockVolatilityData);

      render(
        <MemoryRouter>
          <PriceVolatilityDashboard />
        </MemoryRouter>
      );

      // Verify Header
      expect(screen.getByText(/Biến động Giá Nhập & Giá Bán Theo Mặt Hàng/i)).toBeInTheDocument();

      // Wait for data to load
      await waitFor(() => {
        expect(screen.getByText('SKU-XOAI-3KG')).toBeInTheDocument();
      });

      // Verify KPIs & Badges
      expect(screen.getByText(/Giá nhập gần nhất/i)).toBeInTheDocument();
      expect(screen.getByText(/Giá bán hiện tại/i)).toBeInTheDocument();
      expect(screen.getByText(/Chênh lệch giá \(Spread\)/i)).toBeInTheDocument();
      expect(screen.getByText('Biên lợi nhuận gộp')).toBeInTheDocument();
      expect(screen.getByText('Kinh doanh có lãi')).toBeInTheDocument();
      expect(screen.getByText('Thặng dư giá')).toBeInTheDocument();

      // Verify SKU Name
      expect(screen.getAllByText('Xoài Cát Hòa Lộc Hộp 3kg')[0]).toBeInTheDocument();

      // Verify Transactions Table
      expect(screen.getByText('PO-2026-001')).toBeInTheDocument();
      expect(screen.getByText('Hợp Tác Xã Xoài Tiền Giang')).toBeInTheDocument();

      // Test timeframe switch to "Tuần"
      const btnWeek = screen.getByRole('button', { name: /Tuần/i });
      fireEvent.click(btnWeek);

      await waitFor(() => {
        expect(dashboardApi.getPriceVolatility).toHaveBeenCalledWith(1, 'week');
      });
    });
  });
});
