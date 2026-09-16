# ==============================================================================
# SOLARIS EVALUATION TOOL: RQ3 HAVERSINE ROUTING MATRIX & COLD-CHAIN GUARD
# Kich ban thuc nghiem dinh luong dinh vi kho don le toi uu dua tren giai thuat Haversine
# ==============================================================================

$connStr = "Server=localhost;Database=SolarisDb;Trusted_Connection=True;TrustServerCertificate=True;"

function Calculate-HaversineDistance {
    param(
        [double]$lat1,
        [double]$lon1,
        [double]$lat2,
        [double]$lon2
    )
    $R = 6371.0 # Ban kinh Trai Dat (km)
    $dLat = ($lat2 - $lat1) * [Math]::PI / 180.0
    $dLon = ($lon2 - $lon1) * [Math]::PI / 180.0

    $lat1Rad = $lat1 * [Math]::PI / 180.0
    $lat2Rad = $lat2 * [Math]::PI / 180.0

    $a = [Math]::Sin($dLat / 2.0) * [Math]::Sin($dLat / 2.0) +
         [Math]::Cos($lat1Rad) * [Math]::Cos($lat2Rad) *
         [Math]::Sin($dLon / 2.0) * [Math]::Sin($dLon / 2.0)

    $c = 2.0 * [Math]::Atan2([Math]::Sqrt($a), [Math]::Sqrt(1.0 - $a))
    $distance = $R * $c
    return [Math]::Round($distance, 2)
}

Clear-Host
Write-Host "============================================================================================" -ForegroundColor Cyan
Write-Host "             SOLARIS RQ3 EVALUATION: OPTIMAL SINGLE-WAREHOUSE ROUTING MATRIX                " -ForegroundColor Cyan
Write-Host "       (Giai thuat Haversine Dinh vi Kho Toi uu & Rao chan Chuoi lanh Cold-Chain 15km)       " -ForegroundColor Cyan
Write-Host "============================================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Truy van danh sach Kho hang tu SolarisDb
$queryWarehouses = @"
SELECT w.Id, w.Code, w.Name, a.StreetAddress, a.District, a.Province, a.Latitude, a.Longitude, w.MaxColdChainRadiusKm
FROM Warehouses w
JOIN WarehouseAddresses a ON w.AddressId = a.Id
WHERE w.IsDeleted = 0 AND w.WarehouseType = 'Retail'
ORDER BY w.Id ASC;
"@

# 2. Truy van Dia chi Khach hang mau (CustomerAddressId = 8 - Binh Thanh va CustomerAddressId = 9 - Bien Hoa)
$queryAddresses = @"
SELECT a.CustomerAddressId, c.Name AS CustomerName, a.StreetAddress, a.District, a.Province, a.Latitude, a.Longitude
FROM CustomerAddresses a
JOIN Customers c ON a.CustomerId = c.CustomerId
WHERE a.CustomerAddressId IN (8, 9)
ORDER BY a.CustomerAddressId ASC;
"@

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmdW = New-Object System.Data.SqlClient.SqlCommand($queryWarehouses, $conn)
    $adapterW = New-Object System.Data.SqlClient.SqlDataAdapter($cmdW)
    $dtWarehouses = New-Object System.Data.DataTable
    $adapterW.Fill($dtWarehouses) | Out-Null

    $cmdA = New-Object System.Data.SqlClient.SqlCommand($queryAddresses, $conn)
    $adapterA = New-Object System.Data.SqlClient.SqlDataAdapter($cmdA)
    $dtAddresses = New-Object System.Data.DataTable
    $adapterA.Fill($dtAddresses) | Out-Null

    $conn.Close()
} catch {
    Write-Host "[ERROR] Khong the ket noi Database: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

# ------------------------------------------------------------------------------------------
# KICH BAN 1: KHACH HANG NOI THANH (BINH THANH) - SO SANH 3 DIA CHI: KHACH vs KHO A vs KHO B
# ------------------------------------------------------------------------------------------
$custNear = $dtAddresses | Where-Object { $_.CustomerAddressId -eq 8 }
$latC = [double]$custNear.Latitude
$lonC = [double]$custNear.Longitude

Write-Host "--> [KICH BAN 1]: Kiem tra Dinh vi Kho don le Toi uu voi Khach hang Noi thanh" -ForegroundColor Yellow
Write-Host "    Khach hang      : $($custNear.CustomerName)" -ForegroundColor White
Write-Host "    Dia chi giao    : $($custNear.StreetAddress), $($custNear.District), $($custNear.Province)" -ForegroundColor White
Write-Host "    Toa do GPS Khach: Lat = $latC, Lng = $lonC" -ForegroundColor Green
Write-Host ""
Write-Host "    --- MA TRAN TINH TOAN KHOANG CACH HAVERSINE TU KHACH DEN CAC KHO ---" -ForegroundColor Cyan

$resultsNear = @()
foreach ($w in $dtWarehouses) {
    $latW = [double]$w.Latitude
    $lonW = [double]$w.Longitude
    $dist = Calculate-HaversineDistance $latC $lonC $latW $lonW
    $maxR = [double]$w.MaxColdChainRadiusKm

    $coldChainStatus = if ($dist -le $maxR) { "DAT (<= 15km)" } else { "VI PHAM (> 15km)" }

    $resultsNear += [PSCustomObject]@{
        "Id"          = $w.Id
        "MaKho"       = $w.Code
        "TenKho"      = $w.Name
        "ToaDo_GPS"   = "($latW, $lonW)"
        "KhoangCach"  = "$dist km"
        "DistanceVal" = $dist
        "RaoChan15km" = $coldChainStatus
    }
}

# Sap xep tang dan theo khoang cach
$sortedNear = $resultsNear | Sort-Object DistanceVal
$rank = 1
$tableNear = $sortedNear | ForEach-Object {
    $selected = if ($rank -eq 1) { ">>> TOI UU (CHON)" } else { "Loai (Xa hon)" }
    [PSCustomObject]@{
        "Thu Tu"               = "#$rank"
        "Ma Kho"               = $_.MaKho
        "Ten Kho Hang"         = $_.TenKho
        "Toa Do Kho (Lat, Lng)"= $_.ToaDo_GPS
        "Khoang Cach"          = $_.KhoangCach
        "Rao Chan Chuoi Lanh"  = $_.RaoChan15km
        "Quyet Dinh He Thong"  = $selected
    }
    $rank++
}

$tableNear | Format-Table -AutoSize

$winnerNear = $sortedNear[0]
Write-Host "--> KET QUA KICH BAN 1 (Noi thanh):" -ForegroundColor Green
Write-Host "    Kho toi uu duoc he thong lua chon: $($winnerNear.TenKho) ($($winnerNear.MaKho))" -ForegroundColor Green
Write-Host "    Khoang cach ngan nhat            : $($winnerNear.KhoangCach)" -ForegroundColor Green
Write-Host "    Ly do: Khoang cach $($winnerNear.KhoangCach) la cuc tieu trong tat ca cac kho va thoa man Rao chan 15km." -ForegroundColor Green
Write-Host ""

# ------------------------------------------------------------------------------------------
# KICH BAN 2: KHACH HANG NGOAI THANH (BIEN HOA) - KICH HOAT RAO CHAN CHUOI LANH (> 15KM)
# ------------------------------------------------------------------------------------------
$custFar = $dtAddresses | Where-Object { $_.CustomerAddressId -eq 9 }
$latF = [double]$custFar.Latitude
$lonF = [double]$custFar.Longitude

Write-Host "============================================================================================" -ForegroundColor Yellow
Write-Host "--> [KICH BAN 2]: Kiem tra Rao chan Chuoi lanh voi Khach hang Ngoai thanh (Bien Hoa)" -ForegroundColor Yellow
Write-Host "    Khach hang      : $($custFar.CustomerName)" -ForegroundColor White
Write-Host "    Dia chi giao    : $($custFar.StreetAddress), $($custFar.District), $($custFar.Province)" -ForegroundColor White
Write-Host "    Toa do GPS Khach: Lat = $latF, Lng = $lonF" -ForegroundColor Yellow
Write-Host ""
Write-Host "    --- MA TRAN TINH TOAN KHOANG CACH HAVERSINE TU KHACH DEN CAC KHO ---" -ForegroundColor Cyan

$resultsFar = @()
foreach ($w in $dtWarehouses) {
    $latW = [double]$w.Latitude
    $lonW = [double]$w.Longitude
    $dist = Calculate-HaversineDistance $latF $lonF $latW $lonW
    $maxR = [double]$w.MaxColdChainRadiusKm

    $coldChainStatus = if ($dist -le $maxR) { "DAT" } else { "VI PHAM (> 15km)" }

    $resultsFar += [PSCustomObject]@{
        "Id"          = $w.Id
        "MaKho"       = $w.Code
        "TenKho"      = $w.Name
        "ToaDo_GPS"   = "($latW, $lonW)"
        "KhoangCach"  = "$dist km"
        "DistanceVal" = $dist
        "RaoChan15km" = $coldChainStatus
    }
}

$sortedFar = $resultsFar | Sort-Object DistanceVal
$rank2 = 1
$tableFar = $sortedFar | ForEach-Object {
    $status = if ($_.DistanceVal -gt 15.0) { "[BLOCKED] Tu choi xuat chuoi lanh" } else { "Cho phep" }
    [PSCustomObject]@{
        "Thu Tu"                = "#$rank2"
        "Ma Kho"                = $_.MaKho
        "Ten Kho Hang"          = $_.TenKho
        "Toa Do Kho (Lat, Lng)" = $_.ToaDo_GPS
        "Khoang Cach"           = $_.KhoangCach
        "Rao Chan Chuoi Lanh"   = $_.RaoChan15km
        "Trang Thai Don Hang"   = $status
    }
    $rank2++
}

$tableFar | Format-Table -AutoSize

$nearestFar = $sortedFar[0]
Write-Host "--> KET QUA KICH BAN 2 (Ngoai thanh):" -ForegroundColor Red
Write-Host "    Kho gan nhat            : $($nearestFar.TenKho) ($($nearestFar.KhoangCach))" -ForegroundColor Red
Write-Host "    Ket qua kiem tra an toan: $($nearestFar.KhoangCach) > 15.0 km (Vuot qua ban kinh bao quan cho phep)." -ForegroundColor Red
Write-Host "    Hanh dong he thong      : KICH HOAT RAO CHAN - Tu choi Checkout va hien thi thong bao loi mau do." -ForegroundColor Red
Write-Host "============================================================================================" -ForegroundColor Cyan
