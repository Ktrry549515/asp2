using ShoppingCar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace ShoppingCar.Controllers
{
    public class HomeController : Controller
    {
        //建立可存取dbShoppingCar.mdf資料庫的 dbShoppingCarEntities 類別物件 db
        dbShoppingCarEntities db = new dbShoppingCarEntities();
        // GET: Home
        public ActionResult Index()
        {
            //取得所有產品放入products
            var products = db.tProduct.ToList();
            //若Session["Member"]為空，表示會員未登入
            if (Session["Member"] == null)
            {
                //指定 Index.csthml 套用 _Layout.cshtml View 使用 products
                return View("Index", "_Layout", products);
            }
            //會員登入狀態
            //指定 Index.csthml 套用 _LayoutMember.cshtml View 使用 products
            return View("Index", "_LayoutMember", products);
        }

        //GET:Home/Login
        public ActionResult login()
        {
            return View();
        }

        //POST:Home/Login
        [HttpPost]
        public ActionResult login(string fUserId, string fPwd)
        {
            //依帳號取得會員並指定給member
            var member = db.tMember.Where(m => m.fUserId == fUserId && m.fPwd == fPwd).FirstOrDefault();
            //若member為null 表示會員未註冊
            if (member == null)
            {
                ViewBag.Message = "帳號密碼錯誤 請重新輸入";
                return View();
            }
            //使用Session 變數紀錄歡迎詞
            Session["WelCome"] = member.fName + "歡迎";
            //使用Session 變數紀錄登入的會員物件
            Session["Member"] = member;
            //執行Home 控制器的 Index 動作方法
            return RedirectToAction("Index");
        }
        public ActionResult Register()
        {
            return View();
        }



        //Post:Home/Register
        [HttpPost]
        public ActionResult Register(tMember pMember)
        {
            //若模型沒有通過驗證則顯示目前的View
            if (ModelState.IsValid == false)
            {
                return View();
            }
            // 依帳號取得會員並指定給member
            var member = db.tMember
                .Where(m => m.fUserId == pMember.fUserId)
                .FirstOrDefault();
            //若member為null，表示會員未註冊
            if (member == null)
            {
                //將會員記錄新增到tMember資料表
                db.tMember.Add(pMember);
                db.SaveChanges();
                //執行Home控制器的Login動作方法
                return RedirectToAction("login");
            }
            ViewBag.Message = "此帳號己有人使用，註冊失敗";
            return View();
        }


        //Get:Index/Logout
        public ActionResult logout()
        {
            Session.Clear();  //清除Session變數資料
            // 執行Index方法顯示產品列表
            return RedirectToAction("Index");
        }

        //Get:Index/ShoppingCar
        public ActionResult ShoppingCar()
        {
            //取得登入會員的帳號並指定給fUserId
            string fUserId = (Session["Member"] as tMember).fUserId;
            //找出未成為訂單明細的資料，即購物車內容
            var orderDetails = db.tOrderDetail.Where(m => m.fUserId == fUserId && m.fIsApproved == "否").ToList();
            //指定ShoopingCar.cshtml套用_LayoutMember.cshtml，View使用orderDetails模型
            return View("ShoppingCar", "_LayoutMember", orderDetails);
        }

        //Get:Index/AddCar
        public ActionResult AddCar(string fPId)
        {
            //取得會員帳號並指定給fUserId
            string fUserId = (Session["Member"] as tMember).fUserId;
            //找出會員放入訂單明細的產品 該產品的fIsApproved為"否"
            //表示該產品是購物車狀態
            var currentCar = db.tOrderDetail
                .Where(m => m.fPId == fPId && m.fIsApproved == "否" && m.fUserId == fUserId)
                .FirstOrDefault();

            try
            {
                //若currentCar 等於null 表示會員選購的產品不是購物車狀態
                if (currentCar == null)
                {
                    //找出目前選購的產品並指定給product
                    var product = db.tProduct.Where(m => m.fPId == fPId).FirstOrDefault();
                    //將產品放入訂單明細 因為產品的 fIsApproved 為"否" 表示為購物車狀態
                    tOrderDetail orderDetail = new tOrderDetail();
                    orderDetail.fUserId = fUserId;
                    orderDetail.fPId = product.fPId;
                    orderDetail.fName = product.fName;
                    orderDetail.fPrice = product.fPrice;
                    orderDetail.fQty = 1;
                    orderDetail.fIsApproved = "否";
                    db.tOrderDetail.Add(orderDetail);
                }
                else
                {
                    //若產品為購物車狀態  即將該產品數量加1
                    currentCar.fQty += 1;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                string str_message = ex.Message.ToString();
            }

            //執行Home 控制器 ShoppingCar 動作方法
            return RedirectToAction("ShoppingCar");
        }

        //GET:Index/DeleteCar
        public ActionResult DeleteCar(int fId)
        {
            //依fId找出要刪除的購物車狀態的產品
            var orderDetail = db.tOrderDetail.Where(m => m.fId == fId).FirstOrDefault();
            //刪除購物車狀態的產品
            db.tOrderDetail.Remove(orderDetail);
            db.SaveChanges();
            //執行Home控制器的ShoppingCar動作方法
            return RedirectToAction("ShoppingCar");
        }

        //Post:Index/ShoppingCar 訂單明細
        [HttpPost]
        public ActionResult ShoppingCar(string fReceiver, string fEmail, string fAddress)
        {
            //找出會員帳號並指定給 fUserId
            string fUserId = (Session["Member"] as tMember).fUserId;
            //建立唯一的識別值並指定給 guid 變數，用來當作訂單編號
            // tOrde r的 fOrderGuid 欄位會關聯到 tOrderDetail 的 fOrderGuid 欄位
            //形成一對多的關係，即一比訂單資料會對應到多筆訂單明細
            string guid = Guid.NewGuid().ToString();
            //建立訂單主檔資料
            tOrder order = new tOrder();
            order.fOrderGuid = guid;
            order.fUserId = fUserId;
            order.fReceiver = fReceiver;
            order.fEmail = fEmail;
            order.fAddress = fAddress;
            order.fDate = DateTime.Now;
            db.tOrder.Add(order);
            //找出目前會員在訂單明細中是購物車狀態的產品
            var carList = db.tOrderDetail.Where(m => m.fIsApproved == "否" && m.fUserId == fUserId).ToList();
            //將購物車狀態產品的 fIsApproved 設為"是" 表示確認訂購產品
            foreach (var item in carList)
            {
                item.fOrderGuid = guid;
                item.fIsApproved = "是";
            }
            //更新資料庫 異動 tOrder 和 tOrderDetail 
            //完成訂單主檔和訂單明細的更新
            db.SaveChanges();
            //執行 Home 控制器的 OrderList 動作方法
            return RedirectToAction("OrderList");
        }

        //Get:Home / OrderList
        public ActionResult OrderList()
        {
            //找出會員帳號並指定給fUserId
            string fUserId = (Session["Member"] as tMember).fUserId;
            //找出目前會員的所有訂單主檔紀錄並依照 fDate 進行遞增排序
            //將查詢結果指定給 orders
            var orders = db.tOrder
                .Where(m => m.fUserId == fUserId)
                .OrderByDescending(m => m.fDate)
                .ToList();
            //目前會員的訂單主檔
            //指定 OrderList.cshtml 套用 _LayoutMember.cshtml View 使用 orders 模型 
            return View("OrderList", "_LayoutMember", orders);
        }

        //Get:index / OrderDetail
        public ActionResult OrderDetail(string fOrderGuid)
        {
            //根據fOrderGuid 找出和訂單主檔關聯的訂單明細 並指定給 orderDetails
            var orderDetails = db.tOrderDetail
                .Where(m => m.fOrderGuid == fOrderGuid)
                .ToList();
            //目前訂單明細
            //指定 OrderDetail.cshtml 套用 _LayoutMember.cshtml View 使用 orderDetails 模型
            return View("OrderDetail", "_LayoutMember", orderDetails);
        }
    }
}